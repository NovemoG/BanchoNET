using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.HubClients.Metadata;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Players;
using Novelog.Abstractions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class MetadataHub(ILogger logger, ILazerPlayerService playerService) : BaseHub<IMetadataClient>(logger)
{
    private const string PresenceWatchersGroup = "presence-watchers";
    private static string UserPresenceGroup(int userId) => $"presence:{userId}";
    
    private static readonly ConcurrentDictionary<int, UserPresence> ConnectedUsers = new();
    
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
        if (!ConnectedUsers.TryGetValue(userId, out var presence)) return;

        presence.Activity = activity;
        ConnectedUsers[userId] = presence;
        
        await Clients.Group(PresenceWatchersGroup)
            .UserPresenceUpdated(userId, presence);

        await Clients.Group(UserPresenceGroup(userId))
            .FriendPresenceUpdated(userId, presence);
        
        Logger.LogDebug($"{userId} updated their activity to {activity?.GetType()}");
    }

    public async Task UpdateStatus(
        UserStatus? status
    ) {
        if (!TryGetUserId(out var userId)) return;
        if (!ConnectedUsers.TryGetValue(userId, out var presence)) return;
        
        presence.Status = status;
        ConnectedUsers[userId] = presence;

        await Clients.Group(PresenceWatchersGroup)
            .UserPresenceUpdated(userId, presence);

        await Clients.Group(UserPresenceGroup(userId))
            .FriendPresenceUpdated(userId, presence);
        
        Logger.LogDebug($"{userId} updated their status to {status}");
    }

    public async Task BeginWatchingUserPresence() {
        await Groups.AddToGroupAsync(Context.ConnectionId, PresenceWatchersGroup);
        Logger.LogDebug("Invoked");
    }

    public async Task EndWatchingUserPresence() {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, PresenceWatchersGroup);
        Logger.LogDebug("Invoked");
    }

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

    public async Task RefreshFriends(
        IPlayersRepository players
    ) {
        if (!TryGetUserId(out var userId)) return;

        var prevFriends = playerService.GetPlayer(userId)?.Friends ?? [];
        var friends = await players.GetPlayerFriends(userId);
        
        foreach (var friendId in friends.Select(f => f.TargetId))
        {
            if (prevFriends.Contains(friendId)) continue;
            
            await Groups.AddToGroupAsync(Context.ConnectionId, UserPresenceGroup(friendId));
            
            if (ConnectedUsers.TryGetValue(friendId, out var presence))
                await Clients.Caller.FriendPresenceUpdated(friendId, presence);
        }
        
        playerService.AssignFriends(userId, friends.Select(f => f.TargetId));
        
        Logger.LogDebug("Invoked");
    }

    public override Task OnConnectedAsync() {
        if (TryGetUserId(out var userId))
            ConnectedUsers.TryAdd(userId, new UserPresence());
        
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(
        Exception exception
    ) {
        if (TryGetUserId(out var userId))
        {
            ConnectedUsers.TryRemove(userId, out _);
            playerService.RemovePlayer(userId);
        }
        
        return base.OnDisconnectedAsync(exception);
    }
}