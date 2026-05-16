using System.Globalization;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Db;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BanchoNET.BeatmapServer.Services;

public class BeatmapSearchService(IDbContextFactory<BanchoDbContext> dbFactory) : IBeatmapSearchService
{
    public async Task<(List<ApiBeatmapsetFull>, long)> SearchAsync(
        string? q,
        string? mode,
        string? category,
        string? status,
        string? genre,
        string? language,
        string? extra,
        string? rankAchieved,
        string? sort,
        string? played,
        bool? nsfw,
        Dictionary<string, string>? cursor,
        CancellationToken ct = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var (sortColumn, isDesc) = sort?.ToLower() switch
        {
            "favourites_desc" => ("\"Favorites\"", true),
            "favourites_asc" => ("\"Favorites\"", false),
            "plays_desc" => ("\"Plays\"", true),
            "plays_asc" => ("\"Plays\"", false),
            "rating_desc" => ("\"Rating\"", true),
            "rating_asc" => ("\"Rating\"", false),
            "ranked_desc" => ("\"RankedDate\"", true),
            "ranked_asc" => ("\"RankedDate\"", false),
            "updated_desc" => ("\"LastUpdated\"", true),
            "updated_asc" => ("\"LastUpdated\"", false),
            "difficulty_desc" => ("\"StarRating\"", true),
            "difficulty_asc" => ("\"StarRating\"", false),
            "artist_desc" => ("\"Artist\"", true),
            "artist_asc" => ("\"Artist\"", false),
            "title_desc" => ("\"Title\"", true),
            "title_asc" => ("\"Title\"", false),
            "relevance_asc" => ("rank", false),
            _ => ("rank", true)
        };
        
        var sortDir = isDesc ? "DESC" : "ASC";
        var operatorSign = isDesc ? "<" : ">";
        var orderByClause = $"{sortColumn} {sortDir}, \"SetId\" {sortDir}";
        var cursorClause = cursor != null
            ? $"WHERE ({sortColumn}, \"SetId\") {operatorSign} (@cursorValue, @cursorId)"
            : "";

        var setIdsSql = $"""
                         WITH matched AS (
                             SELECT
                                 "Id",
                                 "SetId",
                                 "Artist",
                                 "Title",
                                 "CreatorName",
                                 "GenreId",
                                 "LanguageId",
                                 "Status",
                                 "Mode",
                                 "Bpm",
                                 "StarRating",
                                 "Plays",
                                 "Favorites",
                                 "Version",
                                 "MaxCombo",
                                 "TotalLength",
                                 "Rating",
                                 "RankedDate",
                                 "LastUpdated",
                                 COALESCE(NULLIF(ts_rank_cd("SearchVector", websearch_to_tsquery('simple', @q)), 0), 0.1) AS rank
                                FROM "BeatmapSearch"
                             WHERE (
                                 @q = '' 
                                 OR "SearchVector" @@ websearch_to_tsquery('simple', @q)
                                 OR CONCAT_WS(' ', "Title", "TitleUnicode", "Artist", "ArtistUnicode", "Version", "CreatorName", "Source", "Tags") ILIKE '%' || @q || '%'
                             )
                             AND (@mode IS NULL OR "Mode" = @mode)
                             AND (
                                 @status IS NULL OR
                                     "Status" = ANY (
                                         CASE @status
                                             WHEN 'graveyard' THEN ARRAY[-2]::smallint[]
                                             WHEN 'wip' THEN ARRAY[-1]::smallint[]
                                             WHEN 'pending' THEN ARRAY[0]::smallint[]
                                             WHEN 'ranked' THEN ARRAY[1, 2]::smallint[]
                                             WHEN 'qualified' THEN ARRAY[3]::smallint[]
                                             WHEN 'loved' THEN ARRAY[4]::smallint[]
                                             WHEN 'leaderboard' THEN ARRAY[1, 2, 3, 4]::smallint[]
                                             ELSE ARRAY["Status"]::smallint[]
                                         END
                                     )
                             )
                             AND (@genre IS NULL OR "GenreId" = @genre)
                             AND (@language IS NULL OR "LanguageId" = @language)
                             AND (@nsfw::bool IS NULL OR "Nsfw" = @nsfw::bool)
                             AND (@played IS NULL OR "Plays" >= @played)
                         ),
                         per_set AS (
                             SELECT
                                 "SetId",
                                 MAX(rank) AS rank,
                                 MAX("Favorites") AS "Favorites",
                                 MAX("Plays") AS "Plays",
                                 MAX("Rating") AS "Rating",
                                 MAX("RankedDate") AS "RankedDate",
                                 MAX("LastUpdated") AS "LastUpdated",
                                 MAX("StarRating") AS "StarRating",
                                 MAX("Title") AS "Title",
                                 MAX("Artist") AS "Artist"
                             FROM matched
                             GROUP BY "SetId"
                         ),
                         counted AS (
                             SELECT *, count(*) OVER() AS "TotalCount"
                             FROM per_set
                         )
                         SELECT "SetId", "TotalCount"
                         FROM counted
                         {cursorClause}
                         ORDER BY {orderByClause}
                         LIMIT 50;
                         """;

        var parsedCursor = ParseCursor(cursor, sort);
        var setIds = new List<int>();
        var totalCount = 0L;

        await using (var cmd = db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = setIdsSql;
            
            cmd.Parameters.Add(new NpgsqlParameter("q", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = q ?? ""
            });

            cmd.Parameters.Add(new NpgsqlParameter("mode", NpgsqlTypes.NpgsqlDbType.Smallint)
            {
                Value = byte.TryParse(mode, out var m) ? m : DBNull.Value
            });
            
            cmd.Parameters.Add(new NpgsqlParameter("status", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = (object?)status ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("genre", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = int.TryParse(genre, out var g) ? g : DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("language", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = int.TryParse(language, out var l) ? l : DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("nsfw", NpgsqlTypes.NpgsqlDbType.Boolean)
            {
                Value = (object?)(nsfw is true or null ? null : false) ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("played", NpgsqlTypes.NpgsqlDbType.Bigint)
            {
                Value = played switch
                {
                    "played" => (long)1,
                    "unplayed" => (long)0,
                    _ => DBNull.Value
                }
            });

            if (parsedCursor != null)
            {
                cmd.Parameters.Add(new NpgsqlParameter("cursorId", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = parsedCursor.Id
                });
        
                cmd.Parameters.Add(new NpgsqlParameter("cursorValue", parsedCursor.DbType)
                {
                    Value = parsedCursor.Value
                });
            }
            
            if (cmd.Connection != null && cmd.Connection.State != System.Data.ConnectionState.Open)
                await cmd.Connection.OpenAsync(ct);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                setIds.Add(reader.GetInt32(0));

                if (totalCount == 0)
                    totalCount = reader.GetInt64(1);
            }
        }
        
        if (setIds.Count == 0)
            return ([], 0);
        
        var beatmapsets = await db.Beatmapsets
            .AsNoTracking()
            .AsSplitQuery()
            .Include(bs => bs.Beatmaps)
                .ThenInclude(b => b.Owners)
                    .ThenInclude(bo => bo.Player)
            .Include(bs => bs.BeatmapsetFavorites)
                .ThenInclude(bf => bf.Player)
            .Include(bs => bs.Creator)
            .Where(bs => setIds.Contains(bs.Id))
            .ToListAsync(cancellationToken: ct);
        
        var orderedBeatmapsets = setIds
            .Join(beatmapsets, id => id, bs => bs.Id, (_, bs) => bs)
            .Select(bs => new ApiBeatmapsetFull(bs))
            .ToList();

        return (orderedBeatmapsets, totalCount);
    }

    private static PaginationCursor? ParseCursor(
        Dictionary<string, string>? cursor,
        string? sort
    ) {
        if (cursor == null || !cursor.TryGetValue("id", out var idStr) || !int.TryParse(idStr, out var id))
            return null;
        
        var valueString = cursor.FirstOrDefault(k => k.Key.ToLower() != "id").Value;
        if (string.IsNullOrEmpty(valueString)) return null;

        return sort?.ToLower() switch
        {
            "difficulty_desc" or "difficulty_asc" or "rating_desc" or "rating_asc" or "relevance_desc" or "relevance_asc" => 
                double.TryParse(valueString, CultureInfo.InvariantCulture, out var d) 
                    ? new PaginationCursor { Id = id, Value = d, DbType = NpgsqlTypes.NpgsqlDbType.Real } 
                    : null,
            
            "plays_desc" or "plays_asc" => 
                long.TryParse(valueString, out var l) 
                    ? new PaginationCursor { Id = id, Value = l, DbType = NpgsqlTypes.NpgsqlDbType.Bigint } 
                    : null,
            
            "favourites_desc" or "favourites_asc" => 
                int.TryParse(valueString, out var i) 
                    ? new PaginationCursor { Id = id, Value = i, DbType = NpgsqlTypes.NpgsqlDbType.Integer } 
                    : null,
            
            "ranked_desc" or "ranked_asc" or "updated_desc" or "updated_asc" => 
                long.TryParse(valueString, CultureInfo.InvariantCulture, out var dt) 
                    ? new PaginationCursor { Id = id, Value = DateTimeOffset.FromUnixTimeMilliseconds(dt), DbType = NpgsqlTypes.NpgsqlDbType.TimestampTz }
                    : null,
            
            _ => new PaginationCursor { Id = id, Value = valueString, DbType = NpgsqlTypes.NpgsqlDbType.Text }
        };
    }

    private class PaginationCursor
    {
        public int Id { get; set; }
        public object Value { get; set; } = null!;
        public NpgsqlTypes.NpgsqlDbType DbType { get; set; }
    }
}