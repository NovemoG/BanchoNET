using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Db;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BanchoNET.BeatmapServer.Services;

public class BeatmapSearchService(IDbContextFactory<BanchoDbContext> dbFactory) : IBeatmapSearchService
{
    public async Task<List<ApiBeatmapsetFull>> SearchAsync(
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
        CancellationToken ct = default
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        
        var orderByClause = sort?.ToLower() switch
        {
            "favourites_desc" => "\"Favorites\" DESC",
            "favourites_asc"  => "\"Favorites\" ASC",
            "plays_desc"      => "\"Plays\" DESC",
            "plays_asc"       => "\"Plays\" ASC",
            "rating_desc"     => "\"Rating\" DESC",
            "rating_asc"      => "\"Rating\" ASC",
            "ranked_desc"     => "\"RankedDate\" DESC",
            "ranked_asc"      => "\"RankedDate\" ASC",
            "updated_desc"    => "\"LastUpdated\" DESC",
            "updated_asc"     => "\"LastUpdated\" ASC",
            "difficulty_desc" => "\"StarRating\" DESC",
            "difficulty_asc"  => "\"StarRating\" ASC",
            "artist_desc"     => "\"Artist\" DESC",
            "artist_asc"      => "\"Artist\" ASC",
            "title_desc"      => "\"Title\" DESC",
            "title_asc"       => "\"Title\" ASC",
            "relevance_asc"   => "rank ASC",
            //"relevance_desc"  => "rank DESC",
            _                 => "rank DESC"
        };

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
                                 OR CONCAT_WS(' ', "Title", "TitleUnicode", "Artist", "ArtistUnicode", "Version", "Source", "Tags", "CreatorName") ILIKE '%' || @q || '%'
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
                         best_per_set AS (
                             SELECT DISTINCT ON ("SetId")
                                 "SetId",
                                 "Favorites",
                                 "Plays",
                                 "Rating",
                                 "RankedDate",
                                 "LastUpdated",
                                 "StarRating",
                                 "Title",
                                 "Artist",
                                 rank
                             FROM matched
                             ORDER BY "SetId", rank DESC, "Plays" DESC, "StarRating" DESC
                         )
                         SELECT "SetId"
                         FROM best_per_set
                         ORDER BY {orderByClause}
                         LIMIT 50;
                         """;

        var setIds = new List<int>();

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
            
            if (cmd.Connection != null && cmd.Connection.State != System.Data.ConnectionState.Open)
                await cmd.Connection.OpenAsync(ct);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                setIds.Add(reader.GetInt32(0));
        }
        
        if (setIds.Count == 0)
            return [];
        
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
        
        return beatmapsets.Select(bs => new ApiBeatmapsetFull(bs)).ToList();
    }
}