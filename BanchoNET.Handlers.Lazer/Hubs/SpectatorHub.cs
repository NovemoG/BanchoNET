using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.HubClients.Spectator;
using BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;
using BanchoNET.Core.Abstractions.Services.Lazer;
using Novelog.Abstractions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class SpectatorHub(ILogger logger) : BaseHub<ISpectatorClient>(logger)
{
    private static string SpectatorGroup(int userId) => $"spectator:{userId}";
    
    public async Task BeginPlaySession(
        long? scoreToken,
        SpectatorState state
    ) {
        if (!TryGetUserId(out var userId)) return;

        await Clients.Group(SpectatorGroup(userId))
            .UserBeganPlaying(userId, state);
        
        Logger.LogDebug("Invoked");
    }

    public async Task SendFrameData(
        FrameDataBundle data
    ) {
        if (!TryGetUserId(out var userId)) return;
        
        //TODO it is not getting invoked
        await Clients.Group(SpectatorGroup(userId))
            .UserSentFrames(userId, data);
        
        Logger.LogDebug("Invoked");
    }

    public async Task EndPlaySession(
        SpectatorState state
    ) {
        if (!TryGetUserId(out var userId)) return;
        
        await Clients.Group(SpectatorGroup(userId))
            .UserFinishedPlaying(userId, state);
        
        Logger.LogDebug("Invoked");
    }

    public async Task StartWatchingUser(
        int userId, //target
        ILazerPlayerService players
    ) {
        if (!TryGetUserId(out var spectatorId)) return;
        if (spectatorId == userId) return;
        
        var spectator = players.GetPlayer(spectatorId)!;
        var spectators = new[] { new SpectatorPlayer
        {
            OnlineId = spectatorId,
            Username = spectator.Player.Username
        }};
        
        var spectatorGroup = SpectatorGroup(userId);
        
        await Clients.User(userId.ToString()).UserStartedWatching(spectators);
        await Clients.Group(spectatorGroup).UserStartedWatching(spectators);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, spectatorGroup);
        
        Logger.LogDebug("Invoked");
    }

    public async Task EndWatchingUser(
        int userId //target
    ) {
        if (!TryGetUserId(out var spectatorId)) return;
        if (spectatorId == userId) return;
        
        var spectatorGroup = SpectatorGroup(userId);
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, spectatorGroup);
        
        await Clients.User(userId.ToString()).UserEndedWatching(spectatorId);
        await Clients.Group(spectatorGroup).UserEndedWatching(spectatorId);
        
        Logger.LogDebug("Invoked");
    }
}