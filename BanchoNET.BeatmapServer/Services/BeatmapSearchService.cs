using System.Globalization;
using System.Text.RegularExpressions;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Db;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BanchoNET.BeatmapServer.Services;

public partial class BeatmapSearchService(IDbContextFactory<BanchoDbContext> dbFactory) : IBeatmapSearchService
{
    private static readonly Dictionary<string, (string Column, NpgsqlTypes.NpgsqlDbType Type)> ColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "bpm", ("\"Bpm\"", NpgsqlTypes.NpgsqlDbType.Real) },
        { "star", ("\"StarRating\"", NpgsqlTypes.NpgsqlDbType.Real) },
        { "stars", ("\"StarRating\"", NpgsqlTypes.NpgsqlDbType.Real) },
        { "difficulty", ("\"StarRating\"", NpgsqlTypes.NpgsqlDbType.Real) },
        { "plays", ("\"Plays\"", NpgsqlTypes.NpgsqlDbType.Bigint) },
        { "favorites", ("\"Favorites\"", NpgsqlTypes.NpgsqlDbType.Integer) },
        { "favourites", ("\"Favorites\"", NpgsqlTypes.NpgsqlDbType.Integer) },
        { "rating", ("\"Rating\"", NpgsqlTypes.NpgsqlDbType.Real) },
    
        { "title", ("\"Title\"", NpgsqlTypes.NpgsqlDbType.Text) },
        { "version", ("\"Version\"", NpgsqlTypes.NpgsqlDbType.Text) },
        { "diff", ("\"Version\"", NpgsqlTypes.NpgsqlDbType.Text) },
        { "artist", ("\"Artist\"", NpgsqlTypes.NpgsqlDbType.Text) },
        { "source", ("\"Source\"", NpgsqlTypes.NpgsqlDbType.Text) },
        { "creator", ("\"CreatorName\"", NpgsqlTypes.NpgsqlDbType.Text) },
        { "mapper", ("\"CreatorName\"", NpgsqlTypes.NpgsqlDbType.Text) }
    };
    
    public async Task<(List<ApiBeatmapsetFull>, long, Dictionary<string, string>)> SearchAsync(
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
        int count = 50,
        int? skip = null,
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
            "ranked_desc" => ("COALESCE(\"RankedDate\", \"LastUpdated\")", true),
            "ranked_asc" => ("COALESCE(\"RankedDate\", \"LastUpdated\")", false),
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

        var cleanQuery = q ?? "";
        var matches = AttributeRegex().Matches(cleanQuery);

        foreach (Match match in matches)
        {
            cleanQuery = cleanQuery.Replace(match.Value, "");
        }
        cleanQuery = cleanQuery.Trim();
        
        var extraWhereClauses = new List<string>();
        var extraParameters = new List<NpgsqlParameter>();
        var paramCounter = 0;
        
        foreach (Match match in matches)
        {
            var key = match.Groups[1].Value;
            var op = match.Groups[2].Value;
            var val = !string.IsNullOrEmpty(match.Groups[3].Value) ? match.Groups[3].Value :
                !string.IsNullOrEmpty(match.Groups[4].Value) ? match.Groups[4].Value :
                match.Groups[5].Value;

            if (ColumnMap.TryGetValue(key, out var target))
            {
                paramCounter++;
                var paramName = $"custom_filter_{paramCounter}";

                if (target.Type == NpgsqlTypes.NpgsqlDbType.Text)
                {
                    extraWhereClauses.Add($"AND {target.Column} ILIKE @{paramName}");
                    extraParameters.Add(new NpgsqlParameter(paramName, target.Type) { Value = $"%{val}%" });
                }
                else
                {
                    if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericVal))
                    {
                        extraWhereClauses.Add($"AND {target.Column} {op} @{paramName}");

                        object castedValue = target.Type switch
                        {
                            NpgsqlTypes.NpgsqlDbType.Bigint => (long)numericVal,
                            NpgsqlTypes.NpgsqlDbType.Integer => (int)numericVal,
                            _ => (float)numericVal
                        };
                        extraParameters.Add(new NpgsqlParameter(paramName, target.Type) { Value = castedValue });
                    }
                }
            }
        }
        
        int? numericId = int.TryParse(cleanQuery, out var parsedId) ? parsedId : null;
        var dynamicFiltersSql = extraWhereClauses.Count > 0 ? string.Join("\n", extraWhereClauses) : "";

        var sortDir = isDesc ? "DESC" : "ASC";
        var operatorSign = isDesc ? "<" : ">";
        var orderByClause = $"{sortColumn} {sortDir}, \"SetId\" {sortDir}";
        var cursorClause = cursor != null
            ? $"WHERE ({sortColumn}, \"SetId\") {operatorSign} (@cursorValue, @cursorId)"
            : "";
        var skipClause = skip != null ? $" OFFSET {skip} ROWS" : "";

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
                                  CASE 
                                      WHEN @numericId IS NOT NULL AND "Id" = @numericId THEN 1000.0
                                      WHEN @numericId IS NOT NULL AND "SetId" = @numericId THEN 750.0
                                      ELSE COALESCE(NULLIF(ts_rank_cd("SearchVector", websearch_to_tsquery('simple', @q)), 0), 0.1)
                                  END AS rank
                                  FROM "BeatmapSearch"
                              WHERE (
                                  @q = ''
                                  OR (@numericId IS NOT NULL AND ("Id" = @numericId OR "SetId" = @numericId))
                                  OR "SearchVector" @@ websearch_to_tsquery('simple', @q)
                                  OR CONCAT_WS(' ', "Title", "TitleUnicode", "Artist", "ArtistUnicode", "CreatorName", "Version", "Source", "Tags") ILIKE '%' || @q || '%'
                              )
                              {dynamicFiltersSql}
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
                              AND (@nsfw IS NULL OR "Nsfw" = @nsfw)
                              AND (@played IS NULL OR "Plays" >= @played)
                          ),
                          per_set AS (
                              SELECT
                                  "SetId",
                                  MAX(rank) AS rank,
                                  {GetAgg("\"Favorites\"")}("Favorites") AS "Favorites",
                                  {GetAgg("\"Plays\"")}("Plays") AS "Plays",
                                  {GetAgg("\"Rating\"")}("Rating") AS "Rating",
                                  {GetAgg("\"RankedDate\"")}("RankedDate") AS "RankedDate",
                                  {GetAgg("\"LastUpdated\"")}("LastUpdated") AS "LastUpdated",
                                  {GetAgg("\"StarRating\"")}("StarRating") AS "StarRating",
                                  {GetAgg("\"Title\"")}("Title") AS "Title",
                                  {GetAgg("\"Artist\"")}("Artist") AS "Artist"
                              FROM matched
                              GROUP BY "SetId"
                          ),
                          counted AS (
                              SELECT *, count(*) OVER() AS "TotalCount"
                              FROM per_set
                          )
                          SELECT "SetId", "TotalCount", rank
                          FROM counted
                          {cursorClause}
                          ORDER BY {orderByClause}
                          LIMIT {count}{skipClause};
                          """;

        var parsedCursor = ParseCursor(cursor, sort);
        var setIds = new List<int>();

        var totalCount = 0L;
        float? lastRank = null;

        await using (var cmd = db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = setIdsSql;
            
            cmd.Parameters.Add(new NpgsqlParameter("q", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = cleanQuery
            });
            
            cmd.Parameters.Add(new NpgsqlParameter("numericId", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = (object?)numericId ?? DBNull.Value
            });
            
            foreach (var extraParam in extraParameters)
            {
                cmd.Parameters.Add(extraParam);
            }

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
                
                if (!reader.IsDBNull(2))
                    lastRank = reader.GetFloat(2);
            }
        }
        
        if (setIds.Count == 0)
            return ([], 0, BuildCursor(last: null, sort, lastRank: null));
        
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

        return (orderedBeatmapsets, totalCount, BuildCursor(orderedBeatmapsets.LastOrDefault(), sort, lastRank));

        string GetAgg(string column) => sortColumn.Contains(column) ? (isDesc ? "MAX" : "MIN") : "MAX";
    }
    
    private static Dictionary<string, string> BuildCursor(
        ApiBeatmapsetFull? last,
        string? sort,
        float? lastRank
    ) {
        var cursor = new Dictionary<string, string>
        {
            ["id"] = (last?.Id ?? 0).ToString(CultureInfo.InvariantCulture)
        };

        switch (sort?.ToLowerInvariant())
        {
            case "artist_desc":
            case "artist_asc":
                cursor["artist.raw"] = last?.Artist ?? "";
                break;

            case "title_desc":
            case "title_asc":
                cursor["title.raw"] = last?.Title ?? "";
                break;

            case "difficulty_desc":
                cursor["beatmaps.difficultyrating"] = last?.Beatmaps
                    .OrderByDescending(b => b.DifficultyRating)
                    .First().DifficultyRating.ToString("0.####", CultureInfo.InvariantCulture)
                    ?? "0";
                break;
            case "difficulty_asc":
                cursor["beatmaps.difficultyrating"] = last?.Beatmaps
                    .OrderBy(b => b.DifficultyRating)
                    .First().DifficultyRating.ToString("0.####", CultureInfo.InvariantCulture)
                    ?? "0";
                break;

            case "plays_desc":
            case "plays_asc":
                cursor["play_count"] = last?.PlayCount.ToString(CultureInfo.InvariantCulture) ?? "0";
                break;

            case "favourites_desc":
            case "favourites_asc":
                cursor["favourite_count"] = last?.FavouriteCount.ToString(CultureInfo.InvariantCulture) ?? "0";
                break;

            case "ranked_desc":
            case "ranked_asc":
                cursor["approved_date"] = last?.RankedDate?.ToUnixTimeMilliseconds().ToString()
                                          ?? last?.LastUpdated.ToUnixTimeMilliseconds().ToString()!;
                break;

            case "updated_desc":
            case "updated_asc":
                cursor["approved_date"] = last?.LastUpdated.ToUnixTimeMilliseconds().ToString() ?? "0";
                break;

            case "rating_desc":
            case "rating_asc":
                cursor["rating"] = last?.Rating.ToString(CultureInfo.InvariantCulture) ?? "0";
                break;

            default:
                cursor["_score"] = lastRank?.ToString(CultureInfo.InvariantCulture) ?? "0";
                break;
        }

        return cursor;
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

    [GeneratedRegex("""(\w+)\s*(=|<=|>=|<|>)\s*(?:"([^"]*)"|'([^']*)'|(\S+))""", RegexOptions.Compiled)]
    private static partial Regex AttributeRegex();
}