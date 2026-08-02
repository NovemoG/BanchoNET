using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IMultiplayerHistoryRepository
{
    /// <returns>The id of the new match, which becomes the lobby's public id.</returns>
    Task<long> CreateMatch(
        string name,
        int? hostId,
        byte mode,
        DateTimeOffset startTime,
        CancellationToken ct = default
    );

    /// <summary>
    /// Marks the match over and closes the stream with <c>MatchDisbanded</c>.
    /// </summary>
    Task CloseMatch(
        long matchId,
        DateTimeOffset endTime,
        CancellationToken ct = default
    );

    Task SetHost(
        long matchId,
        int? hostId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Records a started map and places it in the stream.
    /// </summary>
    /// <returns>The id of the new game, which incoming scores are attributed to.</returns>
    Task<long> StartGame(
        MultiplayerGameDto game,
        CancellationToken ct = default
    );

    /// <param name="aborted">The map was cut short by a referee.</param>
    /// <param name="forceCompleted">The completion timeout fired while clients were still playing.</param>
    Task CloseGame(
        long gameId,
        DateTimeOffset endTime,
        bool aborted = false,
        bool forceCompleted = false,
        CancellationToken ct = default
    );
    
    Task AppendScore(
        MultiplayerScoreDto score,
        CancellationToken ct = default
    );
    
    Task RecordJoin(
        long matchId,
        int playerId,
        CancellationToken ct = default
    );

    Task RecordLeave(
        long matchId,
        int playerId,
        bool kicked = false,
        CancellationToken ct = default
    );

    Task<List<MultiplayerMatchDto>> GetMatches(
        long? beforeId,
        int limit,
        CancellationToken ct = default
    );
    
    Task<MultiplayerMatchDto?> GetMatch(
        long matchId,
        CancellationToken ct = default
    );

    Task<List<MultiplayerMatchDto>> GetPlayerMatches(
        int playerId,
        int offset,
        int limit,
        CancellationToken ct = default
    );

    Task<int> GetPlayerMatchesCount(
        int playerId,
        CancellationToken ct = default
    );
}