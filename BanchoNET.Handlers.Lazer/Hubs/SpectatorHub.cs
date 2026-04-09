using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.HubClients.Spectator;
using BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;
using Novelog.Abstractions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class SpectatorHub(ILogger logger) : BaseHub<ISpectatorClient>(logger)
{
    public async Task BeginPlaySession(
        long? scoreToken,
        SpectatorState state
    ) {
        Logger.LogDebug("Invoked");
    }

    public async Task SendFrameData(
        FrameDataBundle data
    ) {
        Logger.LogDebug("Invoked");
    }

    public async Task EndPlaySession(
        SpectatorState state
    ) {
        Logger.LogDebug("Invoked");
    }

    public async Task StartWatchingUser(
        int userId
    ) {
        Logger.LogDebug("Invoked");
    }

    public async Task EndWatchingUser(
        int userId
    ) {
        Logger.LogDebug("Invoked");
    }
}