using BanchoNET.Core.Abstractions.HubClients.Spectator;
using BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;

namespace BanchoNET.Core.Abstractions.HubClients;

public interface ISpectatorClient
{
    Task UserBeganPlaying(
        int userId,
        SpectatorState state
    );

    Task UserFinishedPlaying(
        int userId,
        SpectatorState state
    );

    Task UserSentFrames(
        int userId,
        FrameDataBundle data
    );

    Task UserScoreProcessed(
        int userId,
        long scoreId
    );

    Task UserStartedWatching(
        SpectatorPlayer[] user
    );

    Task UserEndedWatching(
        int userId
    );
}