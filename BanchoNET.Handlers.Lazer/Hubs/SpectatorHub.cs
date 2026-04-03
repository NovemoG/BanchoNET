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
        
    }

    public async Task SendFrameData(
        FrameDataBundle data
    ) {
        
    }

    public async Task EndPlaySession(
        SpectatorState state
    ) {
        
    }

    public async Task StartWatchingUser(
        int userId
    ) {
        
    }

    public async Task EndWatchingUser(
        int userId
    ) {
        
    }
}