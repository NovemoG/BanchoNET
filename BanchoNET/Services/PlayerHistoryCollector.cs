using System.Diagnostics;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.History;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BanchoNET.Services;

public class PlayerHistoryCollector(
    BanchoDbContext dbContext,
    IConnectionMultiplexer redis,
    IPlayerHistoryRepository playerHistories,
    ILogger logger
) : IPlayerHistoryCollector
{
    private const int BatchSize = 2000;

    public async Task<int> Collect(
        CancellationToken ct = default
    ) {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // A monthly bucket is dated to the month it describes but carries the lifetime counter, so
        // sample(June) - sample(May) is exactly June's activity. Every run rewrites the bucket and
        // AppendSamples keeps the first write, so the value that sticks is the one taken by the
        // first run of the following month. A missed run self-heals the next day, at the cost of
        // counting those extra days into the closing month.
        var monthlyBucket = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);

        logger.LogInfo(
            $"Appending player history (daily {today}, monthly {monthlyBucket})",
            caller: nameof(PlayerHistoryCollector)
        );

        var stopwatch = Stopwatch.StartNew();
        var inserted = 0;

        foreach (var mode in ModeExtensions.TrackedModes)
        {
            if (ct.IsCancellationRequested) break;

            inserted += await CollectMode(mode, today, monthlyBucket, ct);
        }

        stopwatch.Stop();

        logger.LogInfo(
            $"Finished appending player history: {inserted} samples in {stopwatch.Elapsed}",
            caller: nameof(PlayerHistoryCollector)
        );

        return inserted;
    }

    private async Task<int> CollectMode(
        byte mode,
        DateOnly dailyBucket,
        DateOnly monthlyBucket,
        CancellationToken ct
    ) {
        var stopwatch = Stopwatch.StartNew();

        var ranks = await BuildRankMap(mode);

        var lastPlayerId = 0;
        var inserted = 0;
        var samples = new List<PlayerHistorySample>(BatchSize * 4);

        while (!ct.IsCancellationRequested)
        {
            var batch = await dbContext.Stats
                .AsNoTracking()
                .Where(s => s.Mode == mode
                            && s.PlayCount > 0
                            && (s.Player.Privileges & 1) == 1
                            && s.PlayerId > lastPlayerId)
                .OrderBy(s => s.PlayerId)
                .Take(BatchSize)
                .Select(s => new
                {
                    s.PlayerId,
                    s.PP,
                    s.PlayCount,
                    s.ReplayViews
                })
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            lastPlayerId = batch[^1].PlayerId;
            samples.Clear();

            foreach (var row in batch)
            {
                // A player missing from the leaderboard gets no rank sample
                if (ranks.TryGetValue(row.PlayerId, out var rank))
                {
                    samples.Add(new PlayerHistorySample(
                        row.PlayerId, mode, HistoryMetric.GlobalRank, HistoryGranularity.Daily, dailyBucket, rank
                    ));
                }

                samples.Add(new PlayerHistorySample(
                    row.PlayerId, mode, HistoryMetric.Pp, HistoryGranularity.Daily, dailyBucket, row.PP
                ));

                samples.Add(new PlayerHistorySample(
                    row.PlayerId, mode, HistoryMetric.PlayCount, HistoryGranularity.Monthly, monthlyBucket, row.PlayCount
                ));

                samples.Add(new PlayerHistorySample(
                    row.PlayerId, mode, HistoryMetric.ReplayViews, HistoryGranularity.Monthly, monthlyBucket, row.ReplayViews
                ));
            }

            inserted += await playerHistories.AppendSamples(samples, ct);
        }

        stopwatch.Stop();

        logger.LogDebug(
            $"Player history for mode {mode}: {inserted} samples in {stopwatch.Elapsed}",
            caller: nameof(PlayerHistoryCollector)
        );

        return inserted;
    }

    /// <summary>
    /// Reads the whole leaderboard into memory so each player's rank is a lookup rather than a
    /// round trip. Absence from the map means the player is unranked.
    /// </summary>
    private async Task<Dictionary<int, int>> BuildRankMap(
        byte mode
    ) {
        var db = redis.GetDatabase();
        var key = $"bancho:leaderboard:{mode}";
        var total = await db.SortedSetLengthAsync(key);

        var ranks = new Dictionary<int, int>((int)total);
        const int page = 10_000;

        for (long start = 0; start < total; start += page)
        {
            var entries = await db.SortedSetRangeByRankAsync(
                key: key,
                start: start,
                stop: start + page - 1,
                order: Order.Descending
            );

            for (var i = 0; i < entries.Length; i++)
            {
                if (int.TryParse((string?)entries[i], out var playerId))
                    ranks[playerId] = (int)(start + i) + 1;
            }
        }

        return ranks;
    }
}