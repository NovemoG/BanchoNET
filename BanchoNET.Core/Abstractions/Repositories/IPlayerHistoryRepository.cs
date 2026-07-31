using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IPlayerHistoryRepository
{
    Task<int> AppendSamples(
        IReadOnlyList<PlayerHistorySample> samples,
        CancellationToken ct = default
    );
    
    Task<List<PlayerHistoryDto>> GetSeries(
        int playerId,
        byte mode,
        IReadOnlyList<HistoryMetric> metrics,
        HistoryGranularity granularity,
        DateOnly? from = null,
        CancellationToken ct = default
    );
    
    Task<Dictionary<int, PlayerHistoryDto>> GetLatestSamples(
        byte mode,
        HistoryMetric metric,
        HistoryGranularity granularity,
        CancellationToken ct = default
    );
    
    Task<bool> HasAnySamples(
        CancellationToken ct = default
    );
    
    Task<Dictionary<int, double>> GetSamplesAsOf(
        IReadOnlyList<int> playerIds,
        byte mode,
        HistoryMetric metric,
        HistoryGranularity granularity,
        DateOnly asOf,
        CancellationToken ct = default
    );
    
    Task<DateOnly?> GetEarliestSampleDate(
        HistoryGranularity granularity,
        CancellationToken ct = default
    );
}