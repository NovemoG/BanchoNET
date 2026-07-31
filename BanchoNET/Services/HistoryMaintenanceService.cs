using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Maintenance;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public class HistoryMaintenanceService(
    BanchoDbContext dbContext,
    IHistoriesRepository histories,
    IPlayerHistoryRepository playerHistories,
    ILogger logger
) : IHistoryMaintenanceService
{
    private const int BatchSize = 2000;

    public async Task<LifetimeCounterBackfillReport> BackfillLifetimeCounters(
        bool dryRun,
        bool force = false,
        CancellationToken ct = default
    ) {
        var report = new LifetimeCounterBackfillReport
        {
            DryRun = dryRun,
            RunAt = DateTime.UtcNow
        };

        var previous = await dbContext.MaintenanceState
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == IHistoryMaintenanceService.LifetimeCounterBackfillKey, ct);

        if (previous != null)
        {
            report.AlreadyApplied = true;
            report.PreviouslyAppliedAt = previous.LastRunAt;
        }

        // Applying twice would add the recovered totals on top of themselves.
        var blocked = previous != null && !dryRun && !force;
        if (blocked)
        {
            report.Warnings.Add(
                $"Backfill was already applied at {previous!.LastRunAt:u}. Re-applying would double-count. " +
                "Pass force=true only if the previous run is known to have failed partway."
            );

            return report;
        }

        foreach (var mode in ModeExtensions.TrackedModes)
        {
            report.Modes.Add(await BackfillMode(mode, dryRun, ct));
        }

        report.StatsRowsScanned = report.Modes.Sum(m => m.StatsRowsScanned);
        report.StatsRowsAffected = report.Modes.Sum(m => m.StatsRowsAffected);
        report.PlayCountRecovered = report.Modes.Sum(m => m.PlayCountRecovered);
        report.ReplayViewsRecovered = report.Modes.Sum(m => m.ReplayViewsRecovered);
        report.RowsMissingHistory = report.Modes.Sum(m => m.RowsMissingHistory);

        if (report.RowsMissingHistory > 0)
        {
            report.Warnings.Add(
                $"{report.RowsMissingHistory} stats rows hold a non-zero counter but have no history document. " +
                "Those players were reset without ever being snapshotted (the monthly job skipped inactive " +
                "players but reset them anyway) — the lost amount is unrecoverable."
            );
        }

        var duplicates = report.Modes.Sum(m => m.DuplicateHistoryDocuments);
        if (duplicates > 0)
        {
            report.Warnings.Add(
                $"{duplicates} duplicate history documents were found and summed together. " +
                "Nothing enforces uniqueness on (PlayerId, Mode) in Mongo."
            );
        }

        if (dryRun) return report;

        await RecordApplied(
            IHistoryMaintenanceService.LifetimeCounterBackfillKey,
            report.RunAt,
            $"rows={report.StatsRowsAffected} " +
            $"playCount=+{report.PlayCountRecovered} " +
            $"replayViews=+{report.ReplayViewsRecovered}",
            ct
        );

        logger.LogInfo(
            $"Lifetime counter backfill applied: {report.StatsRowsAffected} rows, " +
            $"+{report.PlayCountRecovered} play count, +{report.ReplayViewsRecovered} replay views.",
            caller: nameof(HistoryMaintenanceService)
        );

        return report;
    }

    public async Task<HistoryImportReport> ImportMongoHistories(
        bool dryRun,
        bool force = false,
        CancellationToken ct = default
    ) {
        var report = new HistoryImportReport
        {
            DryRun = dryRun,
            RunAt = DateTime.UtcNow
        };

        var previous = await dbContext.MaintenanceState
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == IHistoryMaintenanceService.MongoHistoryImportKey, ct);

        if (previous != null)
        {
            report.AlreadyImported = true;
            report.PreviouslyImportedAt = previous.LastRunAt;
        }

        if (previous != null && !dryRun && !force)
        {
            report.Warnings.Add(
                $"History was already imported at {previous!.LastRunAt:u}. Re-importing is safe " +
                "(existing samples are kept) but pointless unless the source changed. Pass force=true to proceed."
            );

            return report;
        }

        var today = DateOnly.FromDateTime(report.RunAt);
        
        var earliestDaily = await playerHistories.GetEarliestSampleDate(HistoryGranularity.Daily, ct);
        var earliestMonthly = await playerHistories.GetEarliestSampleDate(HistoryGranularity.Monthly, ct);

        report.DailyAnchor = earliestDaily?.AddDays(-1) ?? today;

        report.MonthlyAnchor = earliestMonthly?.AddMonths(-1)
                               ?? new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        
        var knownPlayers = (await dbContext.Players
                .AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync(ct))
            .ToHashSet();

        foreach (var mode in ModeExtensions.TrackedModes)
        {
            report.Modes.Add(await ImportMode(mode, knownPlayers, report, dryRun, ct));
        }

        report.DocumentsRead = report.Modes.Sum(m =>
            m.RankDocuments + m.PlayCountDocuments + m.ReplayViewsDocuments);
        report.SamplesGenerated = report.Modes.Sum(m =>
            m.RankSamples + m.PlayCountSamples + m.ReplayViewsSamples);
        report.SamplesInserted = report.Modes.Sum(m => m.SamplesInserted);

        var duplicates = report.Modes.Sum(m => m.DuplicatePlayers);
        if (duplicates > 0)
        {
            report.Warnings.Add(
                $"{duplicates} players hold more than one history document. Only the first was " +
                "imported; nothing enforces uniqueness on (PlayerId, Mode) in Mongo."
            );
        }

        var orphaned = report.Modes.Sum(m => m.OrphanedDocuments);
        if (orphaned > 0)
        {
            report.Warnings.Add(
                $"{orphaned} documents reference a player id that no longer exists and were skipped."
            );
        }

        report.Warnings.Add(
            "Imported dates are reconstructed by counting backwards from the newest bucket and are " +
            "only exact if no sampling run was ever missed. Samples written after this import are properly dated."
        );

        report.Warnings.Add(
            $"Anchored so imported history ends at {report.DailyAnchor} (daily) and " +
            $"{report.MonthlyAnchor} (monthly), immediately before the oldest live sample."
        );

        if (dryRun) return report;

        await RecordApplied(
            IHistoryMaintenanceService.MongoHistoryImportKey,
            report.RunAt,
            $"documents={report.DocumentsRead} inserted={report.SamplesInserted}",
            ct
        );

        logger.LogInfo(
            $"Mongo history import applied: {report.DocumentsRead} documents, " +
            $"{report.SamplesInserted} samples inserted.",
            caller: nameof(HistoryMaintenanceService)
        );

        return report;
    }

    public async Task<PeakRankRecomputeReport> RecomputePeakRanks(
        bool dryRun,
        bool force = false,
        CancellationToken ct = default
    ) {
        var report = new PeakRankRecomputeReport
        {
            DryRun = dryRun,
            RunAt = DateTime.UtcNow
        };

        var previous = await dbContext.MaintenanceState
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == IHistoryMaintenanceService.PeakRankRecomputeKey, ct);

        if (previous != null)
        {
            report.AlreadyApplied = true;
            report.PreviouslyAppliedAt = previous.LastRunAt;
        }

        // The pass itself is idempotent; the guard exists so a full-table update doesn't run
        // on every boot
        if (previous != null && !dryRun && !force)
        {
            report.Warnings.Add(
                $"Peak ranks were already recomputed at {previous!.LastRunAt:u}. " +
                "Bump PeakRankRecomputeKey or pass force=true to run it again."
            );

            return report;
        }
        
        const string best =
            """
            SELECT DISTINCT ON ("PlayerId", "Mode") "PlayerId", "Mode", "Value", "Date"
            FROM "PlayerHistories"
            WHERE "Metric" = {0} AND "Granularity" = {1} AND "Value" > 0
            ORDER BY "PlayerId", "Mode", "Value" ASC, "Date" ASC
            """;
        
        const string predicate =
            """s."PlayCount" > 0 AND (s."PeakRank" = 0 OR b."Value"::int < s."PeakRank")""";
        
        const string clearPredicate = "\"PlayCount\" = 0 AND \"PeakRank\" <> 0";

        if (dryRun)
        {
            var countSql =
                $"""
                 SELECT COUNT(*)::int AS "Value"
                 FROM "Stats" s
                 JOIN ({best}) b ON s."PlayerId" = b."PlayerId" AND s."Mode" = b."Mode"
                 WHERE {predicate}
                 """;

            report.RowsAffected = await dbContext.Database
                .SqlQueryRaw<int>(countSql, (short)HistoryMetric.GlobalRank, (short)HistoryGranularity.Daily)
                .SingleAsync(ct);

            report.RowsCleared = await dbContext.Stats
                .CountAsync(s => s.PlayCount == 0 && s.PeakRank != 0, ct);
        }
        else
        {
            var updateSql =
                $"""
                 UPDATE "Stats" s
                 SET "PeakRank" = b."Value"::int,
                     "PeakRankDate" = (b."Date"::timestamp AT TIME ZONE 'UTC')
                 FROM ({best}) b
                 WHERE s."PlayerId" = b."PlayerId" AND s."Mode" = b."Mode" AND {predicate}
                 """;

            report.RowsAffected = await dbContext.Database.ExecuteSqlRawAsync(
                updateSql, [(short)HistoryMetric.GlobalRank, (short)HistoryGranularity.Daily], ct
            );

            report.RowsCleared = await dbContext.Database.ExecuteSqlRawAsync(
                $"""UPDATE "Stats" SET "PeakRank" = 0, "PeakRankDate" = NULL WHERE {clearPredicate}""",
                ct
            );

            await RecordApplied(
                IHistoryMaintenanceService.PeakRankRecomputeKey,
                report.RunAt,
                $"recomputed={report.RowsAffected} cleared={report.RowsCleared}",
                ct
            );

            logger.LogInfo(
                $"Recomputed peak rank for {report.RowsAffected} stats rows, " +
                $"cleared {report.RowsCleared} on never-played modes.",
                caller: nameof(HistoryMaintenanceService)
            );
        }

        report.Warnings.Add(
            "Peak rank dates taken from imported samples are only as accurate as the import itself, " +
            "which reconstructs dates by counting backwards from the newest bucket."
        );

        return report;
    }

    private async Task<ModeImportReport> ImportMode(
        byte mode,
        HashSet<int> knownPlayers,
        HistoryImportReport report,
        bool dryRun,
        CancellationToken ct
    ) {
        var modeReport = new ModeImportReport { Mode = mode };
        var samples = new List<PlayerHistorySample>();

        var rankDocs = await histories.GetRankHistories(mode);
        var playCountDocs = await histories.GetPlayCountHistories(mode);
        var replayDocs = await histories.GetReplaysHistories(mode);

        modeReport.RankDocuments = rankDocs.Count;
        modeReport.PlayCountDocuments = playCountDocs.Count;
        modeReport.ReplayViewsDocuments = replayDocs.Count;
        
        foreach (var (playerId, entries) in Deduplicate(
                     rankDocs.Select(d => (d.PlayerId, d.Entries)), knownPlayers, modeReport))
        {
            var before = samples.Count;

            samples.AddRange(PlayerHistorySeries.FromAbsoluteEntries(
                playerId,
                mode,
                HistoryMetric.GlobalRank,
                HistoryGranularity.Daily,
                entries,
                report.DailyAnchor
            ));

            modeReport.RankSamples += samples.Count - before;
        }
        
        modeReport.PlayCountSamples = ImportPeriodCounts(
            samples, playCountDocs.Select(d => (d.PlayerId, d.Entries)),
            mode, HistoryMetric.PlayCount, report.MonthlyAnchor, knownPlayers, modeReport
        );

        modeReport.ReplayViewsSamples = ImportPeriodCounts(
            samples, replayDocs.Select(d => (d.PlayerId, d.Entries)),
            mode, HistoryMetric.ReplayViews, report.MonthlyAnchor, knownPlayers, modeReport
        );

        if (!dryRun)
            modeReport.SamplesInserted = await playerHistories.AppendSamples(samples, ct);

        return modeReport;
    }

    private static int ImportPeriodCounts(
        List<PlayerHistorySample> samples,
        IEnumerable<(int PlayerId, List<int> Entries)> documents,
        byte mode,
        HistoryMetric metric,
        DateOnly anchor,
        HashSet<int> knownPlayers,
        ModeImportReport modeReport
    ) {
        var before = samples.Count;

        foreach (var (playerId, entries) in Deduplicate(documents, knownPlayers, modeReport))
        {
            samples.AddRange(PlayerHistorySeries.FromPeriodCounts(
                playerId,
                mode,
                metric,
                HistoryGranularity.Monthly,
                entries,
                anchor
            ));
        }

        return samples.Count - before;
    }

    private static IEnumerable<(int PlayerId, List<int> Entries)> Deduplicate(
        IEnumerable<(int PlayerId, List<int> Entries)> documents,
        HashSet<int> knownPlayers,
        ModeImportReport modeReport
    ) {
        var seen = new HashSet<int>();

        foreach (var (playerId, entries) in documents)
        {
            if (!knownPlayers.Contains(playerId))
            {
                modeReport.OrphanedDocuments++;
                continue;
            }

            if (!seen.Add(playerId))
            {
                modeReport.DuplicatePlayers++;
                continue;
            }

            if (entries is { Count: > 0 })
                yield return (playerId, entries);
        }
    }

    /// <summary>
    /// Records that a one-off pass has run, so it is skipped on subsequent startups.
    /// </summary>
    private async Task RecordApplied(
        string key,
        DateTime runAt,
        string details,
        CancellationToken ct
    ) {
        var state = await dbContext.MaintenanceState.FirstOrDefaultAsync(s => s.Key == key, ct);

        if (state == null)
        {
            dbContext.MaintenanceState.Add(new MaintenanceStateDto
            {
                Key = key,
                LastRunAt = runAt,
                Details = details
            });
        }
        else
        {
            state.LastRunAt = runAt;
            state.Details = details;
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task<ModeBackfillReport> BackfillMode(
        byte mode,
        bool dryRun,
        CancellationToken ct
    ) {
        var modeReport = new ModeBackfillReport { Mode = mode };
        
        var playCountDocs = await histories.GetPlayCountHistories(mode);
        var replayDocs = await histories.GetReplaysHistories(mode);

        modeReport.DuplicateHistoryDocuments =
            playCountDocs.Count - playCountDocs.Select(d => d.PlayerId).Distinct().Count() +
            replayDocs.Count - replayDocs.Select(d => d.PlayerId).Distinct().Count();

        var playCounts = playCountDocs
            .GroupBy(d => d.PlayerId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Entries?.Sum() ?? 0));

        var replayViews = replayDocs
            .GroupBy(d => d.PlayerId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Entries?.Sum() ?? 0));

        var lastPlayerId = 0;

        while (!ct.IsCancellationRequested)
        {
            var batch = await dbContext.Stats
                .Where(s => s.Mode == mode && s.PlayerId > lastPlayerId)
                .OrderBy(s => s.PlayerId)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            lastPlayerId = batch[^1].PlayerId;
            modeReport.StatsRowsScanned += batch.Count;

            foreach (var stats in batch)
            {
                var recoveredPlayCount = playCounts.GetValueOrDefault(stats.PlayerId);
                var recoveredReplayViews = replayViews.GetValueOrDefault(stats.PlayerId);

                if (recoveredPlayCount == 0 && recoveredReplayViews == 0)
                {
                    var hasHistory = playCounts.ContainsKey(stats.PlayerId)
                                     || replayViews.ContainsKey(stats.PlayerId);

                    if (!hasHistory && (stats.PlayCount > 0 || stats.ReplayViews > 0))
                        modeReport.RowsMissingHistory++;

                    continue;
                }

                stats.PlayCount += recoveredPlayCount;
                stats.ReplayViews += recoveredReplayViews;

                modeReport.PlayCountRecovered += recoveredPlayCount;
                modeReport.ReplayViewsRecovered += recoveredReplayViews;
                modeReport.StatsRowsAffected++;
            }

            if (!dryRun) await dbContext.SaveChangesAsync(ct);

            dbContext.ChangeTracker.Clear();
        }

        return modeReport;
    }
}