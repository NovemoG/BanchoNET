using BanchoNET.Core.Models.Api.Maintenance;

namespace BanchoNET.Core.Abstractions.Services;

public interface IHistoryMaintenanceService
{
    const string LifetimeCounterBackfillKey = "backfill:lifetime_counters";
    const string MongoHistoryImportKey = "import:mongo_histories";
    const string PeakRankRecomputeKey = "recompute:peak_ranks";

    Task<LifetimeCounterBackfillReport> BackfillLifetimeCounters(
        bool dryRun,
        bool force = false,
        CancellationToken ct = default
    );

    Task<HistoryImportReport> ImportMongoHistories(
        bool dryRun,
        bool force = false,
        CancellationToken ct = default
    );

    Task<PeakRankRecomputeReport> RecomputePeakRanks(
        bool dryRun,
        bool force = false,
        CancellationToken ct = default
    );
}