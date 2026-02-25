using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Coordinators;

public class MultiplayerCoordinator(
    ILogger logger,
    IMultiplayerService matches,
    IChannelService channels,
    IPlayerService players,
    IHistoriesRepository histories
) : IMultiplayerCoordinator
{
    public async Task CreateMatchAsync(
        MultiplayerMatch matchData,
        User player
    ) {
        var matchChannel = new Channel($"multi_{matchData.Id}")
        {
            Description = "This multiplayer's channel.",
            AutoJoin = false,
            Instance = true
        };

        matchData.Id = matches.GetFreeMatchId;
        matchData.LobbyId = await histories.GetMatchId();
        matchData.Chat = matchChannel;
        matchData.Refs.Add(player.Id);
        
        matches.InsertLobby(matchData);
        channels.InsertChannel(matchChannel);

        JoinPlayer(matchData.Id, matchData.Password, player);
        logger.LogDebug($"{player.Username} created a match with ID {matchData.LobbyId}, in-game ID: {matchData.Id}.");
    }
    
    public bool JoinPlayer(
        ushort id,
        string password,
        User player
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
        
        //TODO mediatr player joined match (then mediatr sends chat message)
        //player.SendBotMessage($"Match created by {player.Username} {match.MPLinkEmbed()}", "#multiplayer");
        return true;
    }

    public bool LeavePlayer(
        User player
    ) {
        if (!player.InMatch)
        {
            logger.LogWarning($"{player.Username} tried to leave a match without being in one.");
            return false;
        }

        var lobby = player.Match!;
        var slot = lobby.Slots.First(s => s.Player == player);
		
        slot.Reset(slot.Status == SlotStatus.Locked ? SlotStatus.Locked : SlotStatus.Open);
        channels.LeavePlayer(lobby.Chat, player);

        if (lobby.IsEmpty())
        {
            logger.LogDebug($"Match \"{lobby.Name}\" is empty, removing.");

            lobby.Timer.Stop();
            matches.RemoveLobby(lobby);
            channels.RemoveChannel(lobby.Chat);
            EnqueueDisposeFor(lobby);
        }
        else
        {
            if (lobby.HostId == player.Id)
            {
                var firstOccupiedSlot = lobby.Slots.First(s => s.Player != null);
                lobby.HostId = firstOccupiedSlot.Player!.Id;
                firstOccupiedSlot.Player.Enqueue(new ServerPackets()
                    .MatchTransferHost()
                    .FinalizeAndGetContent());
            }

            if (lobby.CreatorId != player.Id && lobby.Refs.Remove(player.Id)) ;
            //TODO mediatr player joined match (then mediatr sends chat message)
            //lobby.Chat.SendBotMessage($"Removed {player.Username} from match referees.");
			
            EnqueueStateTo(lobby);
        }

        player.Match = null;
        return true;
    }

    public void JoinLobby(
        User player
    ) {
        players.JoinLobby(player);

        foreach (var match in matches.Matches)
        {
            player.Enqueue(new ServerPackets()
                .NewMatch(match)
                .FinalizeAndGetContent());
        }
    }

    public void LeavePlayerToLobby(
        User player
    ) {
        JoinLobby(player);
        channels.JoinPlayer(channels.LobbyChannel, player); 
        LeavePlayer(player);
    }

    public void StartMatch(MultiplayerMatch match)
    {
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

        EnqueueTo(match,
            new ServerPackets().MatchStart(match).FinalizeAndGetContent(),
            noMapPlayerIds,
            false
        );
        EnqueueStateTo(match);
    }

    public void EndMatch(MultiplayerMatch match)
    {
        match.InProgress = false;
        match.ResetPlayersLoadedStatuses();
		
        match.UnreadyPlayers(SlotStatus.Playing | SlotStatus.Ready);

        EnqueueTo(match,
            new ServerPackets()
                .MatchAbort()
                .FinalizeAndGetContent()
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
}