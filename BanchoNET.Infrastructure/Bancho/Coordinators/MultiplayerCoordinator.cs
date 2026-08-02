using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Stable.Multiplayer;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Infrastructure.Bancho.Coordinators;

public class MultiplayerCoordinator(
    ILogger logger,
    IMultiplayerService matches,
    IChannelService channels,
    IPlayerService players,
    IServiceScopeFactory scopeFactory
) : IMultiplayerCoordinator
{
    public async Task<bool> CreateMatchAsync(
        MultiplayerMatch matchData,
        Player player,
        long? channelId = null
    ) {
        var lobbyId = await Record(
            history => history.CreateMatch(
                matchData.Name,
                player.Id,
                (byte)matchData.Mode,
                DateTimeOffset.UtcNow
            ),
            "create match"
        );

        if (lobbyId == null)
        {
            player.Enqueue(new ServerPackets()
                .MatchJoinFail()
                .Notification("Couldn't create the lobby right now. Please try again in a moment.")
                .FinalizeAndGetContent());

            logger.LogWarning($"Refused {player.Username}'s match: could not record it in history.");
            return false;
        }

        matchData.LobbyId = lobbyId.Value;

        // Assigns in-game id for the match
        matches.InsertLobby(matchData);

        var matchChannel = new Channel($"#multi_{matchData.Id}", id: channelId ?? 0, ChannelType.Multiplayer) //TODO
        {
            Description = "This multiplayer's channel.",
            AutoJoin = false,
            Instance = true
        };

        matchData.Chat = matchChannel;
        matchData.Refs.Add(player.Id);

        channels.InsertChannel(matchChannel);

        await JoinPlayer(matchData.Id, matchData.Password, player);

        logger.LogDebug($"{player.Username} created a match with ID {matchData.LobbyId}, in-game ID: {matchData.Id}.");
        return true;
    }

    public async Task<bool> JoinPlayer(
        ushort id,
        string password,
        Player player
    ) {
        var match = matches.GetMatch(id);
        if (match is null)
        {
            player.Enqueue(DefaultPackets.MatchJoinFailData());
            logger.LogDebug($"{player.Username} tried to join match that does not exist.");
            return false;
        }

        if (player.InMatch)
        {
            player.Enqueue(DefaultPackets.MatchJoinFailData());
            logger.LogDebug($"{player.Username} tried to join multiple matches.");
            return false;
        }

        if (match.TourneyClients.Contains(player.Id))
        {
            player.Enqueue(DefaultPackets.MatchJoinFailData());
            return false;
        }

        MultiplayerSlot? slot;
        if (match.HostId != player.Id)
        {
            if (password != match.Password && !player.Privileges.HasFlag(PlayerPrivileges.Staff))
            {
                player.Enqueue(DefaultPackets.MatchJoinFailData());
                logger.LogDebug($"{player.Username} tried to join {match.LobbyId} with incorrect password.");
                return false;
            }

            slot = match.Slots.FirstOrDefault(s => s.Status == SlotStatus.Open);
            if (slot is null)
            {
                player.Enqueue(DefaultPackets.MatchJoinFailData());
                return false;
            }
        }
        else slot = match.Slots[0];

        if (!channels.JoinPlayer(match.Chat, player))
        {
            logger.LogWarning($"{player.Username} failed to join {match.Chat.IdName}");
            return false;
        }

        channels.LeavePlayer(channels.LobbyChannel, player);

        if (match.Type is LobbyType.TeamVS or LobbyType.TagTeamVS)
            slot.Team = LobbyTeams.Red;

        slot.Status = SlotStatus.NotReady;
        slot.Player = player;
        player.Match = match;

        player.Enqueue(new ServerPackets()
            .MatchJoinSuccess(match)
            .FinalizeAndGetContent());
        EnqueueStateTo(match);

        await Record(history => history.RecordJoin(match.LobbyId, player.Id), "record join");

        players.SendBotMessageTo(player, $"Here is the mp link for the match: {match.MPLinkEmbed()}", "#multiplayer");
        return true;
    }

    public async Task<bool> LeavePlayer(
        Player player,
        MultiplayerEventType reason = MultiplayerEventType.PlayerLeft
    ) {
        if (!player.InMatch)
        {
            logger.LogWarning($"{player.Username} tried to leave a match without being in one.");
            return false;
        }

        var match = player.Match!;
        var slot = match.Slots.First(s => s.Player != null && s.Player.Equals(player));

        slot.Reset(slot.Status == SlotStatus.Locked ? SlotStatus.Locked : SlotStatus.Open);
        channels.LeavePlayer(match.Chat, player);

        var disbanded = match.IsEmpty();
        int? newHostId = null;

        if (disbanded)
        {
            logger.LogDebug($"Match \"{match.Name}\" is empty, removing.");

            matches.RemoveLobby(match);
            channels.RemoveChannel(match.Chat);
            EnqueueDisposeFor(match);
        }
        else
        {
            if (match.HostId == player.Id)
            {
                var firstOccupiedSlot = match.Slots.First(s => s.Player != null);
                match.HostId = firstOccupiedSlot.Player!.Id;
                newHostId = match.HostId;

                firstOccupiedSlot.Player.Enqueue(new ServerPackets()
                    .MatchTransferHost()
                    .FinalizeAndGetContent());
            }

            if (match.CreatorId != player.Id && match.Refs.Remove(player.Id))
                channels.SendBotMessageTo(match.Chat, $"Removed {player.Username} from match referees.", players.BanchoBot);

            EnqueueStateTo(match);
        }

        player.Match = null;

        await Record(async history =>
        {
            await history.RecordLeave(match.LobbyId, player.Id, reason == MultiplayerEventType.PlayerKicked);

            if (newHostId.HasValue)
                await history.SetHost(match.LobbyId, newHostId.Value);

            if (disbanded)
                await history.CloseMatch(match.LobbyId, DateTimeOffset.UtcNow);
        }, "record leave");
        
        if (disbanded) match.Dispose();

        return true;
    }

    public void JoinLobby(
        Player player
    ) {
        players.JoinLobby(player);

        foreach (var match in matches.Matches)
        {
            player.Enqueue(new ServerPackets()
                .NewMatch(match)
                .FinalizeAndGetContent());
        }
    }

    public async Task LeavePlayerToLobby(
        Player player,
        MultiplayerEventType reason = MultiplayerEventType.PlayerLeft
    ) {
        JoinLobby(player);
        channels.JoinPlayer(channels.LobbyChannel, player);
        await LeavePlayer(player, reason);
    }

    public void InviteToLobby(
        Player player,
        Player? target
    ) {
        if (target == null) return;
        if (target.IsBot)
        {
            players.SendBotMessageTo(player, "I'm too busy right now! Maybe later 👼");
            return;
        }

        target.Enqueue(new ServerPackets()
            .MatchInvite(player, target.Username)
            .FinalizeAndGetContent());

        logger.LogDebug($"{player.Username} invited {target.Username} to their match.", nameof(MultiplayerExtensions));
    }

    public async Task StartMatch(
        MultiplayerMatch match
    ) {
        // The countdown path fires outside any request, so the lobby may have moved on since
        if (match.InProgress || match.IsEmpty()) return;

        var noMapPlayerIds = new List<int>();

        foreach (var slot in match.Slots)
        {
            if (slot.Player == null) continue;

            if (slot.Status != SlotStatus.NoMap)
                slot.Status = SlotStatus.Playing;
            else
                noMapPlayerIds.Add(slot.Player.Id);
        }

        match.InProgress = true;

        var gameId = await Record(history => history.StartGame(new MultiplayerGameDto
        {
            MatchId = match.LobbyId,
            BeatmapId = match.BeatmapId > 0 ? match.BeatmapId : null,
            BeatmapMD5 = match.BeatmapMD5,
            BeatmapName = match.BeatmapName,
            Mode = (byte)match.Mode,
            WinCondition = match.WinCondition,
            LobbyType = match.Type,
            Mods = match.Freemods ? 0 : (int)match.Mods,
            StartTime = DateTimeOffset.UtcNow
        }), "start game");

        if (gameId.HasValue)
            match.TrackGame(gameId.Value, match.BeatmapMD5);

        match.LoadTimeout.Arm(AppSettings.MultiplayerMapLoadTimeout, () =>
        {
            logger.LogWarning($"Match {match.LobbyId} timed out waiting for clients to load.");
            AllPlayersLoaded(match);
            return Task.CompletedTask;
        });

        EnqueueTo(match,
            new ServerPackets().MatchStart(match).FinalizeAndGetContent(),
            noMapPlayerIds,
            false
        );
        EnqueueStateTo(match);
    }

    public void AllPlayersLoaded(
        MultiplayerMatch match
    ) {
        if (!match.LoadTimeout.Disarm()) return;

        foreach (var slot in match.Slots)
            if (slot.Status == SlotStatus.Playing)
                slot.Loaded = true;

        EnqueueTo(match,
            new ServerPackets().MatchAllPlayersLoaded().FinalizeAndGetContent(),
            toLobby: false
        );
    }

    public async Task AbortMatch(
        MultiplayerMatch match
    ) {
        if (!match.InProgress) return;

        match.InProgress = false;
        match.CompleteTimeout.Disarm();
        match.LoadTimeout.Disarm();

        if (match.CurrentGameId is { } gameId)
            await Record(history => history.CloseGame(gameId, DateTimeOffset.UtcNow, aborted: true), "abort game");

        match.ResetPlayersLoadedStatuses();

        match.UnreadyPlayers(SlotStatus.Playing | SlotStatus.Ready);

        EnqueueTo(match,
            new ServerPackets()
                .MatchAbort()
                .FinalizeAndGetContent()
        );
        EnqueueStateTo(match);
    }

    public async Task CompleteGame(
        MultiplayerMatch match,
        bool forced = false
    ) {
        if (!match.InProgress) return;

        match.InProgress = false;
        match.CompleteTimeout.Disarm();
        match.LoadTimeout.Disarm();

        var notPlayingIds = new List<int>();

        foreach (var slot in match.Slots)
        {
            if (slot.Player == null) continue;

            if (slot.Status != SlotStatus.Complete)
                notPlayingIds.Add(slot.Player.Id);
        }

        if (match.CurrentGameId is { } gameId)
            await Record(history => history.CloseGame(gameId, DateTimeOffset.UtcNow, forceCompleted: forced), "close game");
        
        match.UnreadyPlayers(SlotStatus.Complete | SlotStatus.Playing);
        match.ResetPlayersLoadedStatuses();
        match.InProgress = false;

        EnqueueTo(match,
            new ServerPackets().MatchComplete().FinalizeAndGetContent(),
            notPlayingIds,
            false
        );
        EnqueueStateTo(match);
    }

    public void EnqueueTo(
        MultiplayerMatch match,
        byte[] data,
        List<int>? immune = null,
        bool toLobby = true
    ) {
        match.Chat.EnqueueToPlayers(data, immune);

        if (!toLobby) return;

        foreach (var player in players.PlayersInLobby)
            player.Enqueue(data);
    }

    public void EnqueueStateTo(
        MultiplayerMatch match,
        bool toLobby = true
    ) {
        match.Chat.EnqueueToPlayers(new ServerPackets()
            .UpdateMatch(match, true)
            .FinalizeAndGetContent());

        if (!toLobby) return;

        var data = new ServerPackets()
            .UpdateMatch(match, false)
            .FinalizeAndGetContent();

        foreach (var player in players.PlayersInLobby)
            player.Enqueue(data);
    }

    public void EnqueueDisposeFor(
        MultiplayerMatch match
    ) {
        var data = new ServerPackets()
            .DisposeMatch(match)
            .FinalizeAndGetContent();

        foreach (var player in players.PlayersInLobby)
            player.Enqueue(data);
    }

    #region History
    
    private async Task Record(
        Func<IMultiplayerHistoryRepository, Task> write,
        string what
    ) {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await write(scope.ServiceProvider.GetRequiredService<IMultiplayerHistoryRepository>());
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to {what} in multiplayer history", ex, nameof(MultiplayerCoordinator));
        }
    }

    private async Task<long?> Record(
        Func<IMultiplayerHistoryRepository, Task<long>> write,
        string what
    ) {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            return await write(scope.ServiceProvider.GetRequiredService<IMultiplayerHistoryRepository>());
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to {what} in multiplayer history", ex, nameof(MultiplayerCoordinator));
            return null;
        }
    }

    #endregion
}