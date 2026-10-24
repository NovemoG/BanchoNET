using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Lazer.Metadata;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.SignalR;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class MetadataHub(
    ILogger logger,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps,
    IPlayerService stablePlayers,
    ILazerPlayerService playerService
) : BaseHub<IMetadataClient>(logger)
{
    private const string PresenceWatchersGroup = "presence-watchers";
    private static string UserPresenceGroup(int userId) => $"presence:{userId}";
    
    private static readonly ConcurrentDictionary<int, MetadataClientState> ConnectedUsers = new();
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromSeconds(AppSettings.LazerActivityWriteIntervalInSeconds);
    
    public Task<BeatmapUpdates> GetChangesSince(
        int queueId
    ) {
        return beatmaps.GetBeatmapsetChangesSince(queueId);
    }

    public async Task UpdateActivity(
        UserActivity? activity
    ) {
        if (!TryGetOwnState(out var userId, out var state)) return;

        if (state.Activity == null && activity == null)
            return;

        state.Activity = activity;

        if (state.IsVisible)
            await TouchActivity(userId);

        await Task.WhenAll(
            state.IsVisible
                ? BroadcastUserPresenceUpdate(userId, state.ToUserPresence())
                : Task.CompletedTask,
            Clients.Caller.UserPresenceUpdated(userId, state.ToUserPresence())
        );
    }

    public async Task UpdateStatus(
        UserStatus? status
    ) {
        if (status != null && !Enum.IsDefined(status.Value))
            throw new HubException($"Invalid user status: {status}");

        if (!TryGetOwnState(out var userId, out var state)) return;

        if (state.Status == status) return;
        
        var wasVisible = state.IsVisible;
        state.Status = status;

        await playerService.SetPresenceHidden(userId, !state.IsVisible);

        // mirrors osu-web's hide presence toggle, so /me reports a null last_visit
        // and the client restores "appear offline" on the next login
        if (status != null)
        {
            var hidden = status == UserStatus.Offline;
            await players.UpdatePlayerPresenceHidden(userId, hidden);

            var stablePlayer = stablePlayers.GetPlayer(userId);
            stablePlayer?.AppearOffline = hidden;
        }

        if (state.IsVisible)
            await TouchActivity(userId);
        
        await Task.WhenAll(
            wasVisible || state.IsVisible
                ? BroadcastUserPresenceUpdate(userId, state.ToUserPresence())
                : Task.CompletedTask,
            Clients.Caller.UserPresenceUpdated(userId, state.ToUserPresence())
        );
    }

    public async Task BeginWatchingUserPresence() {
        foreach (var (userId, state) in ConnectedUsers)
        {
            var presence = state.ToUserPresence();
            if (presence == null) continue;
            
            await Clients.Caller.UserPresenceUpdated(userId, presence);
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, PresenceWatchersGroup);
    }

    public async Task EndWatchingUserPresence()
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, PresenceWatchersGroup);

    public async Task<MultiplayerPlaylistItemStats[]> BeginWatchingMultiplayerRoom(
        long id
    ) {
        Logger.LogDebug("Invoked");
        return [];
    }

    public async Task EndWatchingMultiplayerRoom(
        long id
    ) {
        Logger.LogDebug("Invoked");
    }

    public async Task RefreshFriends() {
        if (!TryGetOwnState(out var userId, out var state)) return;
        
        await RefreshFriends(userId, state);
    }

    public override async Task OnConnectedAsync() {
        if (TryGetUserId(out var userId))
        {
            var state = new MetadataClientState(Context.ConnectionId);
            MetadataClientState? previous = null;

            ConnectedUsers.AddOrUpdate(
                userId,
                state,
                (_, existing) => {
                    previous = existing;
                    return state;
                });

            // a newer connection takes over the user's state
            if (previous != null)
            {
                if (previous.IsVisible)
                    await BroadcastUserPresenceUpdate(userId, null);

                await Clients.Client(previous.ConnectionId).DisconnectRequested();
            }

            // hidden until the client reports its status
            await playerService.SetPresenceHidden(userId, true);

            // if server has restarted but player is still logged in
            if (await playerService.GetPlayer(userId) == null)
            {
                var apiPlayer = await players.GetFullPlayerInfo(userId);
                if (apiPlayer != null)
                    await playerService.AddPlayer(apiPlayer);
            }
            
            await RefreshFriends(userId, state);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(
        Exception exception
    ) {
        if (TryGetUserId(out var userId)
            && ConnectedUsers.TryGetValue(userId, out var state)
            && state.ConnectionId == Context.ConnectionId
            && ConnectedUsers.TryRemove(new KeyValuePair<int, MetadataClientState>(userId, state)))
        {
            if (state.IsVisible)
                await TouchActivity(userId, true);

            await playerService.RemovePlayer(userId);
            
            if (state.IsVisible)
                await BroadcastUserPresenceUpdate(userId, null);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    private bool TryGetOwnState(
        out int userId,
        out MetadataClientState state
    ) {
        state = null!;
        if (!TryGetUserId(out userId)) return false;

        if (ConnectedUsers.TryGetValue(userId, out var existing) && existing.ConnectionId == Context.ConnectionId)
        {
            state = existing;
            return true;
        }

        Logger.LogWarning($"Ignored call from stale metadata connection of user: {userId}", caller: GetType().Name);
        return false;
    }

    private async Task RefreshFriends(
        int userId,
        MetadataClientState state
    ) {
        foreach (var friendId in state.FriendIds)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserPresenceGroup(friendId));

        var friendIds = (await players.GetPlayerFriends(userId))
            .Select(f => f.TargetId)
            .Where(id => id != userId)
            .ToArray();

        foreach (var friendId in friendIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserPresenceGroup(friendId));

            if (ConnectedUsers.TryGetValue(friendId, out var friend) && friend.ToUserPresence() is { } presence)
                await Clients.Caller.FriendPresenceUpdated(friendId, presence);
        }

        state.FriendIds = friendIds;
        await playerService.AssignFriends(userId, friendIds);
    }

    private async Task TouchActivity(
        int userId,
        bool force = false
    ) {
        if (!force && !await playerService.WriteActivity(userId, ActivityWriteInterval))
            return;

        await players.UpdateLatestActivity(userId);
    }

    private Task BroadcastUserPresenceUpdate(
        int userId,
        UserPresence? presence
    ) {
        return Task.WhenAll(
            Clients.Group(PresenceWatchersGroup).UserPresenceUpdated(userId, presence),
            Clients.Group(UserPresenceGroup(userId)).FriendPresenceUpdated(userId, presence)
        );
    }

    private sealed class MetadataClientState(
        string connectionId
    ) {
        public string ConnectionId { get; } = connectionId;
        public UserActivity? Activity { get; set; }
        public UserStatus? Status { get; set; }
        public int[] FriendIds { get; set; } = [];

        public bool IsVisible => Status is UserStatus.Online or UserStatus.DoNotDisturb;
        
        public UserPresence? ToUserPresence() => IsVisible
            ? new UserPresence { Activity = Activity, Status = Status }
            : null;
    }
}