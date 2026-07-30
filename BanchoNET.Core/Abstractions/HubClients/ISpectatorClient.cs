using BanchoNET.Core.Models.Lazer.Spectator;
using BanchoNET.Core.Models.Lazer.Spectator.Frames;

namespace BanchoNET.Core.Abstractions.HubClients;

public interface ISpectatorClient : IStatefulUserHubClient
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