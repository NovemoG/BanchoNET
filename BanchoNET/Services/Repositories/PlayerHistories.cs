using System.Text;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class PlayerHistoryRepository(BanchoDbContext dbContext) : IPlayerHistoryRepository
{
    private const int BatchSize = 2000;

    public async Task<int> AppendSamples(
        IReadOnlyList<PlayerHistorySample> samples,
        CancellationToken ct = default
    ) {
        if (samples.Count == 0) return 0;

        var inserted = 0;

        for (var offset = 0; offset < samples.Count; offset += BatchSize)
        {
            var count = Math.Min(BatchSize, samples.Count - offset);
            var parameters = new object[count * 6];
            var values = new StringBuilder();

            for (var i = 0; i < count; i++)
            {
                var sample = samples[offset + i];
                var p = i * 6;

                parameters[p] = sample.PlayerId;
                parameters[p + 1] = (short)sample.Mode;
                parameters[p + 2] = (short)sample.Metric;
                parameters[p + 3] = (short)sample.Granularity;
                parameters[p + 4] = sample.Date;
                parameters[p + 5] = sample.Value;

                if (i > 0) values.Append(',');
                values.Append($"({{{p}}},{{{p + 1}}},{{{p + 2}}},{{{p + 3}}},{{{p + 4}}},{{{p + 5}}})");
            }
            
            var sql = $"""
                       INSERT INTO "PlayerHistories" ("PlayerId", "Mode", "Metric", "Granularity", "Date", "Value")
                       VALUES {values}
                       ON CONFLICT DO NOTHING
                       """;

            inserted += await dbContext.Database.ExecuteSqlRawAsync(sql, parameters, ct);
        }

        return inserted;
    }

    public async Task<List<PlayerHistoryDto>> GetSeries(
        int playerId,
        byte mode,
        IReadOnlyList<HistoryMetric> metrics,
        HistoryGranularity granularity,
        DateOnly? from = null,
        CancellationToken ct = default
    ) {
        var query = dbContext.PlayerHistories
            .AsNoTracking()
            .Where(h => h.PlayerId == playerId
                        && h.Mode == mode
                        && h.Granularity == granularity);

        if (metrics.Count > 0)
        {
            var wanted = metrics.ToArray();
            query = query.Where(h => wanted.Contains(h.Metric));
        }

        if (from.HasValue)
            query = query.Where(h => h.Date >= from.Value);

        return await query
            .OrderBy(h => h.Metric)
            .ThenBy(h => h.Date)
            .ToListAsync(ct);
    }

    public async Task<Dictionary<int, PlayerHistoryDto>> GetLatestSamples(
        byte mode,
        HistoryMetric metric,
        HistoryGranularity granularity,
        CancellationToken ct = default
    ) {
        const string sql =
            """
            SELECT DISTINCT ON ("PlayerId")
                   "PlayerId", "Mode", "Metric", "Granularity", "Date", "Value"
            FROM "PlayerHistories"
            WHERE "Mode" = {0} AND "Metric" = {1} AND "Granularity" = {2}
            ORDER BY "PlayerId", "Date" DESC
            """;

        var rows = await dbContext.PlayerHistories
            .FromSqlRaw(sql, (short)mode, (short)metric, (short)granularity)
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.PlayerId);
    }

    public async Task<Dictionary<int, double>> GetSamplesAsOf(
        IReadOnlyList<int> playerIds,
        byte mode,
        HistoryMetric metric,
        HistoryGranularity granularity,
        DateOnly asOf,
        CancellationToken ct = default
    ) {
        if (playerIds.Count == 0) return [];
        
        const string sql =
            """
            SELECT DISTINCT ON ("PlayerId")
                   "PlayerId", "Mode", "Metric", "Granularity", "Date", "Value"
            FROM "PlayerHistories"
            WHERE "PlayerId" = ANY({0})
              AND "Mode" = {1} AND "Metric" = {2} AND "Granularity" = {3} AND "Date" <= {4}
            ORDER BY "PlayerId", "Date" DESC
            """;

        var rows = await dbContext.PlayerHistories
            .FromSqlRaw(sql, playerIds.ToArray(), (short)mode, (short)metric, (short)granularity, asOf)
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.PlayerId, r => r.Value);
    }

    public async Task<bool> HasAnySamples(
        CancellationToken ct = default
    ) {
        return await dbContext.PlayerHistories.AnyAsync(ct);
    }

    public async Task<DateOnly?> GetEarliestSampleDate(
        HistoryGranularity granularity,
        CancellationToken ct = default
    ) {
        return await dbContext.PlayerHistories
            .Where(h => h.Granularity == granularity)
            .Select(h => (DateOnly?)h.Date)
            .MinAsync(ct);
    }
}