using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IPlayerHistoryRepository
{
    /// <summary>
    /// Stores samples, keeping the value already held by a bucket rather than overwriting it.
    /// <returns>The number of samples actually stored.</returns>
    /// </summary>
    Task<int> AppendSamples(
        IReadOnlyList<PlayerHistorySample> samples,
        CancellationToken ct = default
    );

    /// <summary>
    /// Samples for one player and mode, ordered by metric then date ascending. Every consumer in
    /// <see cref="PlayerHistorySeries"/> depends on that ordering and reads unordered input as
    /// wrong values rather than as an error.
    /// </summary>
    /// <param name="metrics">Metrics to return, or empty for all of them.</param>
    Task<List<PlayerHistoryDto>> GetSeries(
        int playerId,
        byte mode,
        IReadOnlyList<HistoryMetric> metrics,
        HistoryGranularity granularity,
        DateOnly? from = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Each player's newest sample dated at or before <paramref name="asOf"/>. Players with no
    /// sample in range are absent from the result rather than present with a zero.
    /// </summary>
    Task<Dictionary<int, double>> GetSamplesAsOf(
        IReadOnlyList<int> playerIds,
        byte mode,
        HistoryMetric metric,
        HistoryGranularity granularity,
        DateOnly asOf,
        CancellationToken ct = default
    );
}