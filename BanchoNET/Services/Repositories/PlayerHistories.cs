using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class PlayerHistoryRepository(BanchoDbContext dbContext) : IPlayerHistoryRepository
{
    private const int BatchSize = 10_000;

    public async Task<int> AppendSamples(
        IReadOnlyList<PlayerHistorySample> samples,
        CancellationToken ct = default
    ) {
        if (samples.Count == 0) return 0;
        
        const string sql =
            """
            INSERT INTO "PlayerHistories" ("PlayerId", "Mode", "Metric", "Granularity", "Date", "Value")
            SELECT * FROM unnest(
                {0}::integer[], {1}::smallint[], {2}::smallint[],
                {3}::smallint[], {4}::date[], {5}::double precision[]
            )
            ON CONFLICT DO NOTHING
            """;

        var inserted = 0;

        for (var offset = 0; offset < samples.Count; offset += BatchSize)
        {
            var count = Math.Min(BatchSize, samples.Count - offset);

            var playerIds = new int[count];
            var modes = new short[count];
            var metrics = new short[count];
            var granularities = new short[count];
            var dates = new DateOnly[count];
            var values = new double[count];

            for (var i = 0; i < count; i++)
            {
                var sample = samples[offset + i];

                playerIds[i] = sample.PlayerId;
                modes[i] = sample.Mode;
                metrics[i] = (short)sample.Metric;
                granularities[i] = (short)sample.Granularity;
                dates[i] = sample.Date;
                values[i] = sample.Value;
            }
            
            inserted += await dbContext.Database.ExecuteSqlRawAsync(
                sql, [playerIds, modes, metrics, granularities, dates, values], ct
            );
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
}