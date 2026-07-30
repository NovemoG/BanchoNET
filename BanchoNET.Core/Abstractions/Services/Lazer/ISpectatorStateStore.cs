using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Lazer.Spectator.Frames;

namespace BanchoNET.Core.Abstractions.Services.Lazer;

public interface ISpectatorStateStore
{
    Task<ClientSpectatorState?> Get(
        long scoreToken
    );

    Task Store(
        long scoreToken,
        ClientSpectatorState state
    );

    Task Remove(
        long scoreToken
    );
    
    Task<long?> GetActiveToken(
        int userId
    );

    Task SetActiveToken(
        int userId,
        long scoreToken
    );

    Task ClearActiveToken(
        int userId
    );

    Task AppendFrames(
        long scoreToken,
        IList<LegacyReplayFrame> frames
    );

    Task<List<LegacyReplayFrame>> GetFrames(
        long scoreToken
    );
    
    Task MarkPending(
        long scoreToken
    );

    Task ClearPending(
        long scoreToken
    );

    Task<long[]> GetPending();
}