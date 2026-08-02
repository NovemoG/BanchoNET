using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Db;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace BanchoNET.BeatmapServer.Services;

public partial class BeatmapSearchService(IDbContextFactory<BanchoDbContext> dbFactory) : IBeatmapSearchService
{
    private const int MaxQueryTokens = 8;
    
    // Relevance tiers, in the order title > artist > creator > guest owner > version > source > tags.
    private const string TitleScore = "1000.0::float8";
    private const string ArtistScore = "400.0::float8";
    private const string CreatorScore = "160.0::float8";
    private const string ExtraScore = "64.0::float8";
    private const string OwnerBonus = "32.0::float8";
    private const string VersionBonus = "24.0::float8";
    private const string SourceBonus = "8.0::float8";
    private const string TagsBonus = "3.0::float8";
    private const string TieBreakCap = "2.0::float8";
    
    // ts_rank_cd weight array, ordered {D, C, B, A}
    private const string RankWeights = "'{0.1, 0.25, 0.5, 1.0}'::float4[]";
    
    private static readonly Dictionary<string, (string Column, NpgsqlDbType Type)> ColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "bpm", ("\"Bpm\"", NpgsqlDbType.Real) },
        { "star", ("\"StarRating\"", NpgsqlDbType.Real) },
        { "stars", ("\"StarRating\"", NpgsqlDbType.Real) },
        { "difficulty", ("\"StarRating\"", NpgsqlDbType.Real) },
        { "ar", ("\"Ar\"", NpgsqlDbType.Real) },
        { "od", ("\"Od\"", NpgsqlDbType.Real) },
        { "hp", ("\"Hp\"", NpgsqlDbType.Real) },
        { "cs", ("\"Cs\"", NpgsqlDbType.Real) },
        { "key", ("\"Cs\"", NpgsqlDbType.Real) },
        { "keys", ("\"Cs\"", NpgsqlDbType.Real) },
        { "length", ("\"TotalLength\"", NpgsqlDbType.Integer) },
        { "combo", ("\"MaxCombo\"", NpgsqlDbType.Integer) },
        { "maxcombo", ("\"MaxCombo\"", NpgsqlDbType.Integer) },
        { "plays", ("\"SetPlays\"", NpgsqlDbType.Bigint) },
        { "favorites", ("\"Favorites\"", NpgsqlDbType.Integer) },
        { "favourites", ("\"Favorites\"", NpgsqlDbType.Integer) },
        { "rating", ("\"Rating\"", NpgsqlDbType.Real) },
        
        { "title", ("\"Title\"", NpgsqlDbType.Text) },
        { "version", ("\"Version\"", NpgsqlDbType.Text) },
        { "diff", ("\"Version\"", NpgsqlDbType.Text) },
        { "artist", ("\"Artist\"", NpgsqlDbType.Text) },
        { "source", ("\"Source\"", NpgsqlDbType.Text) }
    };

    // Handled separately from ColumnMap, as these match four columns rather than one
    private static readonly HashSet<string> CreatorKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "creator", "mapper", "owner"
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
        int? playerId = null,
        PlayedScope playedScope = PlayedScope.Player,
        int count = 50,
        int? skip = null,
        CancellationToken ct = default
    ) {
        var (sortColumn, isDesc) = sort?.ToLowerInvariant() switch
        {
            "favourites_desc" => ("\"Favorites\"", true),
            "favourites_asc" => ("\"Favorites\"", false),
            "plays_desc" => ("\"SetPlays\"", true),
            "plays_asc" => ("\"SetPlays\"", false),
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

        var extraWhereClauses = new List<string>();
        var extraParameters = new List<NpgsqlParameter>();
        var paramCounter = 0;
        
        var cleanQuery = q ?? "";

        // Only strip a `key<op>value` pair out of the free text when it maps to a real column,
        // so an unrecognised one stays searchable
        foreach (Match match in AttributeRegex().Matches(cleanQuery))
        {
            var key = match.Groups[1].Value;
            var op = match.Groups[2].Value;
            var val = !string.IsNullOrEmpty(match.Groups[3].Value) ? match.Groups[3].Value :
                !string.IsNullOrEmpty(match.Groups[4].Value) ? match.Groups[4].Value :
                match.Groups[5].Value;

            if (CreatorKeys.Contains(key))
            {
                var nameParam = $"custom_filter_{++paramCounter}";
                var idParam = $"custom_filter_{++paramCounter}";

                // Matches the set's creator and the difficulty's owner, by name or by id, so a
                // guest mapper is found on the difficulties they actually made
                extraWhereClauses.Add(
                    $"""
                     AND (
                         s."CreatorName" ILIKE @{nameParam} OR s."OwnerNames" ILIKE @{nameParam}
                         OR (@{idParam} IS NOT NULL AND (s."CreatorId" = @{idParam} OR s."OwnerIds" @> ARRAY[@{idParam}]))
                     )
                     """);

                extraParameters.Add(new NpgsqlParameter(nameParam, NpgsqlDbType.Text)
                {
                    Value = $"%{val}%"
                });
                extraParameters.Add(new NpgsqlParameter(idParam, NpgsqlDbType.Integer)
                {
                    Value = int.TryParse(val, out var ownerId) ? ownerId : DBNull.Value
                });

                cleanQuery = cleanQuery.Replace(match.Value, "");
                continue;
            }

            if (!ColumnMap.TryGetValue(key, out var target))
                continue;

            paramCounter++;
            var paramName = $"custom_filter_{paramCounter}";
            
            if (target.Type == NpgsqlDbType.Text)
            {
                extraWhereClauses.Add($"AND s.{target.Column} ILIKE @{paramName}");
                extraParameters.Add(new NpgsqlParameter(paramName, target.Type) { Value = $"%{val}%" });
            }
            else
            {
                if (!double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericVal))
                    continue;
                
                extraWhereClauses.Add($"AND s.{target.Column} {op} @{paramName}");

                object castedValue = target.Type switch
                {
                    NpgsqlDbType.Bigint => (long)numericVal,
                    NpgsqlDbType.Integer => (int)numericVal,
                    _ => (float)numericVal
                };
                extraParameters.Add(new NpgsqlParameter(paramName, target.Type) { Value = castedValue });
            }
            
            cleanQuery = cleanQuery.Replace(match.Value, "");
        }

        cleanQuery = cleanQuery.Trim();

        var tokens = Tokenize(cleanQuery);
        var tsQuery = BuildTsQuery(tokens, weight: null);
        var likePatterns = tokens.Select(t => $"%{t}%").ToArray();
        
        int? numericId = int.TryParse(cleanQuery, out var parsedId) ? parsedId : null;

        var statuses = ResolveStatuses(status);
        var favouritesOnly = string.Equals(status, "favourites", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(status, "favorites", StringComparison.OrdinalIgnoreCase);
        var mineOnly = string.Equals(status, "mine", StringComparison.OrdinalIgnoreCase);

        // Both of these are meaningless without a player to scope them to.
        if ((favouritesOnly || mineOnly) && playerId == null)
            return ([], 0, BuildCursor(last: null, sort, lastRank: null));

        var filtersSql = BuildFilters(
            extraWhereClauses,
            favouritesOnly,
            mineOnly,
            played,
            playedScope,
            playerId
        );

        var parsedCursor = ParseCursor(cursor, sort);

        var sortDir = isDesc ? "DESC" : "ASC";
        var operatorSign = isDesc ? "<" : ">";
        var orderByClause = $"{sortColumn} {sortDir}, \"SetId\" {sortDir}";
        var cursorClause = parsedCursor != null
            ? $"WHERE ({sortColumn}, \"SetId\") {operatorSign} (@cursorValue, @cursorId)"
            : "";
        var skipClause = skip is > 0 ? $" OFFSET {skip} ROWS" : "";

        var setIdsSql = $"""
                          WITH matched AS (
                              SELECT
                                  s."SetId",
                                  s."Favorites",
                                  s."SetPlays",
                                  s."Rating",
                                  s."RankedDate",
                                  s."LastUpdated",
                                  s."StarRating",
                                  s."Title",
                                  s."Artist",
                                  ({ExactScoreSql})::float8 AS rank
                              FROM "BeatmapSearch" s
                              WHERE (
                                  @tsQuery IS NULL
                                  OR s."SearchVector" @@ @tsQuery::tsquery
                                  OR (@numericId IS NOT NULL AND (s."Id" = @numericId OR s."SetId" = @numericId))
                              )
                              {filtersSql}
                          ),
                          fuzzy AS (
                              SELECT
                                  s."SetId",
                                  s."Favorites",
                                  s."SetPlays",
                                  s."Rating",
                                  s."RankedDate",
                                  s."LastUpdated",
                                  s."StarRating",
                                  s."Title",
                                  s."Artist",
                                  ({FuzzyScoreSql})::float8 AS rank
                              FROM "BeatmapSearch" s
                              WHERE @tsQuery IS NOT NULL
                              AND NOT EXISTS (SELECT 1 FROM matched)
                              AND (
                                  s."Title" %> @rawQuery OR s."TitleUnicode" %> @rawQuery
                                  OR s."Artist" %> @rawQuery OR s."ArtistUnicode" %> @rawQuery
                                  OR s."CreatorName" %> @rawQuery OR s."OwnerNames" %> @rawQuery
                                  OR s."Version" %> @rawQuery OR s."Source" %> @rawQuery OR s."Tags" %> @rawQuery
                              )
                              {filtersSql}
                          ),
                          combined AS (
                              SELECT * FROM matched
                              UNION ALL
                              SELECT * FROM fuzzy
                          ),
                          per_set AS (
                              SELECT
                                  "SetId",
                                  MAX(rank) AS rank,
                                  {GetAgg("\"Favorites\"")}("Favorites") AS "Favorites",
                                  {GetAgg("\"SetPlays\"")}("SetPlays") AS "SetPlays",
                                  {GetAgg("\"Rating\"")}("Rating") AS "Rating",
                                  {GetAgg("\"RankedDate\"")}("RankedDate") AS "RankedDate",
                                  {GetAgg("\"LastUpdated\"")}("LastUpdated") AS "LastUpdated",
                                  {GetAgg("\"StarRating\"")}("StarRating") AS "StarRating",
                                  {GetAgg("\"Title\"")}("Title") AS "Title",
                                  {GetAgg("\"Artist\"")}("Artist") AS "Artist"
                              FROM combined
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

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var setIds = new List<int>();

        var totalCount = 0L;
        double? lastRank = null;

        await using (var cmd = db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = setIdsSql;
            
            cmd.Parameters.Add(new NpgsqlParameter("tsQuery", NpgsqlDbType.Text)
            {
                Value = (object?)tsQuery ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("tsQueryTitle", NpgsqlDbType.Text)
            {
                Value = (object?)BuildTsQuery(tokens, 'A') ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("tsQueryArtist", NpgsqlDbType.Text)
            {
                Value = (object?)BuildTsQuery(tokens, 'B') ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("tsQueryCreator", NpgsqlDbType.Text)
            {
                Value = (object?)BuildTsQuery(tokens, 'C') ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("tsQueryExtra", NpgsqlDbType.Text)
            {
                Value = (object?)BuildTsQuery(tokens, 'D') ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("likePatterns", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = likePatterns
            });

            cmd.Parameters.Add(new NpgsqlParameter("rawQuery", NpgsqlDbType.Text)
            {
                Value = cleanQuery
            });
            
            cmd.Parameters.Add(new NpgsqlParameter("numericId", NpgsqlDbType.Integer)
            {
                Value = (object?)numericId ?? DBNull.Value
            });
            
            foreach (var extraParam in extraParameters)
            {
                cmd.Parameters.Add(extraParam);
            }

            cmd.Parameters.Add(new NpgsqlParameter("mode", NpgsqlDbType.Smallint)
            {
                Value = (object?)ResolveMode(mode) ?? DBNull.Value
            });
            
            cmd.Parameters.Add(new NpgsqlParameter("statuses", NpgsqlDbType.Array | NpgsqlDbType.Smallint)
            {
                Value = (object?)statuses ?? DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("genre", NpgsqlDbType.Integer)
            {
                Value = int.TryParse(genre, out var g) ? g : DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("language", NpgsqlDbType.Integer)
            {
                Value = int.TryParse(language, out var l) ? l : DBNull.Value
            });

            cmd.Parameters.Add(new NpgsqlParameter("includeNsfw", NpgsqlDbType.Boolean)
            {
                Value = nsfw is not false
            });

            cmd.Parameters.Add(new NpgsqlParameter("playerId", NpgsqlDbType.Integer)
            {
                Value = (object?)playerId ?? DBNull.Value
            });

            if (parsedCursor != null)
            {
                cmd.Parameters.Add(new NpgsqlParameter("cursorId", NpgsqlDbType.Integer)
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
                    lastRank = reader.GetDouble(2);
            }
        }
        
        if (setIds.Count == 0)
            return ([], 0, BuildCursor(last: null, sort, lastRank: null));
        
        var beatmapsets = await db.Beatmapsets
            .AsNoTracking()
            .AsSplitQuery()
            .Include(bs => bs.Beatmaps)
                .ThenInclude(b => b.Collaborators)
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
    
    /// <summary>
    /// Exact relevance score. Each tier is a plain <c>@@</c> test against the stored weighted
    /// vector, so a match's tier is decided by which field it landed in rather than by how often
    /// the term occurs.
    /// </summary>
    private const string ExactScoreSql =
        $"""
         CASE
             WHEN @numericId IS NOT NULL AND s."Id" = @numericId THEN 100000.0
             WHEN @numericId IS NOT NULL AND s."SetId" = @numericId THEN 90000.0
             WHEN @tsQuery IS NULL THEN 0.0
             ELSE
                 (CASE WHEN s."SearchVector" @@ @tsQueryTitle::tsquery THEN {TitleScore} ELSE 0.0 END)
                 + (CASE WHEN s."SearchVector" @@ @tsQueryArtist::tsquery THEN {ArtistScore} ELSE 0.0 END)
                 + (CASE WHEN s."SearchVector" @@ @tsQueryCreator::tsquery THEN {CreatorScore} ELSE 0.0 END)
                 + (CASE WHEN s."SearchVector" @@ @tsQueryExtra::tsquery THEN {ExtraScore} ELSE 0.0 END)
                 + (CASE WHEN s."OwnerNames" ILIKE ALL(@likePatterns) THEN {OwnerBonus} ELSE 0.0 END)
                 + (CASE WHEN s."Version" ILIKE ALL(@likePatterns) THEN {VersionBonus} ELSE 0.0 END)
                 + (CASE WHEN s."Source" ILIKE ALL(@likePatterns) THEN {SourceBonus} ELSE 0.0 END)
                 + (CASE WHEN s."Tags" ILIKE ALL(@likePatterns) THEN {TagsBonus} ELSE 0.0 END)
                 + LEAST(ts_rank_cd({RankWeights}, s."SearchVector", @tsQuery::tsquery)::float8, {TieBreakCap})
         END
         """;

    /// <summary>
    /// Fallback score, only ever evaluated when the exact branch matched nothing. Uses the same
    /// tier ordering, scaled by trigram word similarity so the closest spelling comes out on top.
    /// </summary>
    private const string FuzzyScoreSql =
        $"""
         {TitleScore} * GREATEST(word_similarity(@rawQuery, s."Title"), word_similarity(@rawQuery, s."TitleUnicode"))
         + {ArtistScore} * GREATEST(word_similarity(@rawQuery, s."Artist"), word_similarity(@rawQuery, s."ArtistUnicode"))
         + {CreatorScore} * word_similarity(@rawQuery, s."CreatorName")
         + {OwnerBonus} * word_similarity(@rawQuery, s."OwnerNames")
         + {VersionBonus} * word_similarity(@rawQuery, s."Version")
         + {SourceBonus} * word_similarity(@rawQuery, s."Source")
         + {TagsBonus} * word_similarity(@rawQuery, s."Tags")
         """;

    private static string BuildFilters(
        List<string> extraWhereClauses,
        bool favouritesOnly,
        bool mineOnly,
        string? played,
        PlayedScope playedScope,
        int? playerId
    ) {
        var filters = new StringBuilder();

        filters.AppendLine("""AND (@mode IS NULL OR s."Mode" = @mode)""");
        filters.AppendLine("""AND (@statuses IS NULL OR s."Status" = ANY(@statuses))""");
        filters.AppendLine("""AND (@genre IS NULL OR s."GenreId" = @genre)""");
        filters.AppendLine("""AND (@language IS NULL OR s."LanguageId" = @language)""");
        filters.AppendLine("""AND (@includeNsfw OR NOT s."Nsfw")""");

        if (mineOnly)
            filters.AppendLine("""AND s."CreatorId" = @playerId""");

        if (favouritesOnly)
            filters.AppendLine(
                """
                AND EXISTS (
                    SELECT 1 FROM "BeatmapsetFavorites" f
                    WHERE f."BeatmapsetId" = s."SetId" AND f."PlayerId" = @playerId
                )
                """);

        var wantsPlayed = string.Equals(played, "played", StringComparison.OrdinalIgnoreCase);
        var wantsUnplayed = string.Equals(played, "unplayed", StringComparison.OrdinalIgnoreCase);

        if (wantsPlayed || wantsUnplayed)
        {
            if (playedScope == PlayedScope.Anyone)
            {
                filters.AppendLine(wantsPlayed
                    ? """AND s."Plays" > 0"""
                    : """AND s."Plays" = 0""");
            }
            else if (playerId != null)
            {
                filters.AppendLine(
                    $"""
                     AND {(wantsPlayed ? "" : "NOT ")}EXISTS (
                         SELECT 1 FROM "BeatmapPlays" bp
                         WHERE bp."BeatmapId" = s."Id" AND bp."PlayerId" = @playerId
                     )
                     """);
            }
        }

        foreach (var clause in extraWhereClauses)
            filters.AppendLine(clause);

        return filters.ToString();
    }
    
    private static List<string> Tokenize(
        string query
    ) {
        return TokenRegex().Matches(query)
            .Select(m => m.Value.ToLowerInvariant())
            .Distinct()
            .Take(MaxQueryTokens)
            .ToList();
    }
    
    private static string? BuildTsQuery(
        List<string> tokens,
        char? weight
    ) {
        return tokens.Count == 0
            ? null
            : string.Join(" & ", tokens.Select(t => $"'{t}':*{weight}"));
    }
    
    private static short? ResolveMode(
        string? mode
    ) {
        if (string.IsNullOrWhiteSpace(mode))
            return null;

        if (short.TryParse(mode, out var numeric))
            return numeric < 0 ? null : numeric;

        return mode.ToLowerInvariant() switch
        {
            "osu" or "standard" or "std" => (short)GameMode.VanillaStd,
            "taiko" => (short)GameMode.VanillaTaiko,
            "fruits" or "catch" or "ctb" => (short)GameMode.VanillaCatch,
            "mania" => (short)GameMode.VanillaMania,
            _ => null
        };
    }
    
    private static short[]? ResolveStatuses(
        string? status
    ) {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        switch (status.ToLowerInvariant())
        {
            case "graveyard": return [(short)BeatmapStatus.Graveyard];
            case "wip": return [(short)BeatmapStatus.WIP];
            case "pending": return [(short)BeatmapStatus.LatestPending];
            case "ranked": return [(short)BeatmapStatus.Ranked, (short)BeatmapStatus.Approved];
            case "qualified": return [(short)BeatmapStatus.Qualified];
            case "loved": return [(short)BeatmapStatus.Loved];
            case "leaderboard":
                return
                [
                    (short)BeatmapStatus.Ranked,
                    (short)BeatmapStatus.Approved,
                    (short)BeatmapStatus.Qualified,
                    (short)BeatmapStatus.Loved
                ];
            case "any" or "favourites" or "favorites" or "mine": return null;
        }

        if (!short.TryParse(status, out var numeric))
            return null;
        
        return numeric == (short)BeatmapStatus.Ranked
            ? [(short)BeatmapStatus.Ranked, (short)BeatmapStatus.Approved]
            : [numeric];
    }

    private static Dictionary<string, string> BuildCursor(
        ApiBeatmapsetFull? last,
        string? sort,
        double? lastRank
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

        return sort?.ToLowerInvariant() switch
        {
            "difficulty_desc" or "difficulty_asc" or "rating_desc" or "rating_asc" =>
                double.TryParse(valueString, CultureInfo.InvariantCulture, out var d)
                    ? new PaginationCursor { Id = id, Value = d, DbType = NpgsqlDbType.Real }
                    : null,
            
            "plays_desc" or "plays_asc" =>
                long.TryParse(valueString, out var l)
                    ? new PaginationCursor { Id = id, Value = l, DbType = NpgsqlDbType.Bigint }
                    : null,
            
            "favourites_desc" or "favourites_asc" =>
                int.TryParse(valueString, out var i)
                    ? new PaginationCursor { Id = id, Value = i, DbType = NpgsqlDbType.Integer }
                    : null,
            
            "ranked_desc" or "ranked_asc" or "updated_desc" or "updated_asc" =>
                long.TryParse(valueString, CultureInfo.InvariantCulture, out var dt)
                    ? new PaginationCursor { Id = id, Value = DateTimeOffset.FromUnixTimeMilliseconds(dt), DbType = NpgsqlDbType.TimestampTz }
                    : null,
            
            "artist_desc" or "artist_asc" or "title_desc" or "title_asc" =>
                new PaginationCursor { Id = id, Value = valueString, DbType = NpgsqlDbType.Text },
            
            _ => double.TryParse(valueString, CultureInfo.InvariantCulture, out var score)
                ? new PaginationCursor { Id = id, Value = score, DbType = NpgsqlDbType.Double }
                : null
        };
    }

    private class PaginationCursor
    {
        public int Id { get; set; }
        public object Value { get; set; } = null!;
        public NpgsqlDbType DbType { get; set; }
    }

    [GeneratedRegex("""(\w+)\s*(=|<=|>=|<|>)\s*(?:"([^"]*)"|'([^']*)'|(\S+))""", RegexOptions.Compiled)]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();
}