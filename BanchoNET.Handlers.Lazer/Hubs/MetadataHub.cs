using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Lazer.Metadata;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class MetadataHub(
    ILogger logger,
    IPlayersRepository players,
    ILazerPlayerService playerService
) : BaseHub<IMetadataClient>(logger)
{
    private const string PresenceWatchersGroup = "presence-watchers";
    private static string UserPresenceGroup(int userId) => $"presence:{userId}";
    
    private static readonly ConcurrentDictionary<int, UserPresence> ConnectedUsers = new();
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromSeconds(AppSettings.LazerActivityWriteIntervalInSeconds);
    
    public async Task<BeatmapUpdates> GetChangesSince(
        int queueId
    ) {
        Logger.LogDebug("Invoked");
        return new BeatmapUpdates([], -1);
    }

    public async Task UpdateActivity(
        UserActivity? activity
    ) {
        if (!TryGetUserId(out var userId)) return;
        ConnectedUsers.TryGetValue(userId, out var presence);

        if (presence.Activity == null && activity == null)
            return;

        presence.Activity = activity;
        ConnectedUsers[userId] = presence;

        if (presence.Status != UserStatus.Offline)
            await TouchActivity(userId);

        await Task.WhenAll(
            presence.Status != UserStatus.Offline
                ? BroadcastUserPresenceUpdate(userId, presence)
                : Task.CompletedTask,
            Clients.Caller.UserPresenceUpdated(userId, presence)
        );
    }

    public async Task UpdateStatus(
        UserStatus? status
    ) {
        if (!TryGetUserId(out var userId)) return;
        ConnectedUsers.TryGetValue(userId, out var presence);

        if (presence.Status == status) return;
        
        presence.Status = status;
        ConnectedUsers[userId] = presence;

        var hidden = status == UserStatus.Offline;
        await playerService.SetPresenceHidden(userId, hidden);

        if (!hidden)
            await TouchActivity(userId);

        await BroadcastUserPresenceUpdate(userId, hidden ? null : presence);
    }

    public async Task BeginWatchingUserPresence() {
        foreach (var (userId, presence) in ConnectedUsers)
        {
            if (presence.Status == UserStatus.Offline) continue;
            
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
        if (!TryGetUserId(out var userId)) return;

        var friendIds = (await playerService.GetPlayer(userId))?.Friends ?? [];
        foreach (var friendId in friendIds)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserPresenceGroup(friendId));

        friendIds = (await players.GetPlayerFriends(userId))
            .Select(f => f.TargetId)
            .ToArray();
        
        foreach (var friendId in friendIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserPresenceGroup(friendId));
            
            if (ConnectedUsers.TryGetValue(friendId, out var presence) && presence.Status != UserStatus.Offline)
                await Clients.Caller.FriendPresenceUpdated(friendId, presence);
        }
        
        await playerService.AssignFriends(userId, friendIds);
    }

    public override async Task OnConnectedAsync() {
        if (TryGetUserId(out var userId))
        {
            ConnectedUsers.TryAdd(userId, new UserPresence());

            // if server has restarted but player is still logged in
            if (await playerService.GetPlayer(userId) == null)
            {
                var apiPlayer = await players.GetFullPlayerInfo(userId);
                if (apiPlayer != null)
                    await playerService.AddPlayer(apiPlayer);
            }
            
            await RefreshFriends();
            await TouchActivity(userId, true);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(
        Exception exception
    ) {
        if (TryGetUserId(out var userId))
        {
            ConnectedUsers.TryRemove(userId, out var presence);

            if (presence.Status != UserStatus.Offline)
                await TouchActivity(userId, true);

            await playerService.RemovePlayer(userId);
            
            if (presence.Status != UserStatus.Offline)
                await BroadcastUserPresenceUpdate(userId, null);
        }
        
        await base.OnDisconnectedAsync(exception);
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
}