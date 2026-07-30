using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Db;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace BanchoNET.Services;

public class SearchProjectionSyncService : ISearchProjectionSyncService
{
    private const string UpsertSql =
        """
        INSERT INTO "BeatmapSearch"
        ("Id", "SetId", "Mode", "Status", "GenreId", "LanguageId", "Nsfw",
         "StarRating", "Bpm", "Cs", "Ar", "Od", "Hp", "CirclesCount", "SlidersCount", "SpinnersCount",
         "MaxCombo", "TotalLength", "HitLength", "Plays", "SetPlays", "Favorites", "Rating", "Version",
         "Artist", "ArtistUnicode", "Title", "TitleUnicode", "Source", "Tags", "Description",
         "CreatorName", "CreatorId", "OwnerIds", "OwnerNames", "LastUpdated", "SubmittedDate", "RankedDate")
        SELECT
            b."Id",
            b."SetId",
            b."Mode",
            b."Status",
            bs."GenreId",
            bs."LanguageId",
            bs."Nsfw",
            b."StarRating",
            CASE WHEN bs."Bpm" <> 0 THEN bs."Bpm" ELSE b."Bpm" END,
            b."Cs",
            b."Ar",
            b."Od",
            b."Hp",
            b."CirclesCount",
            b."SlidersCount",
            b."SpinnersCount",
            b."MaxCombo",
            b."TotalLength",
            b."HitLength",
            b."Plays",
            bs."PlayCount",
            bs."FavoriteCount",
            bs."Rating",
            b."Version",
            bs."Artist",
            bs."ArtistUnicode",
            bs."Title",
            bs."TitleUnicode",
            bs."Source",
            bs."Tags",
            bs."Description",
            bs."CreatorName",
            bs."CreatorId",
            array_remove(ARRAY[b."OwnerId"] || COALESCE((
                SELECT array_agg(c."OwnerId")
                FROM "BeatmapCollaborators" c
                WHERE c."BeatmapId" = b."Id"
            ), ARRAY[]::integer[]), 0),
            b."OwnerName" || COALESCE((
                SELECT ' ' || string_agg(c."OwnerName", ' ')
                FROM "BeatmapCollaborators" c
                WHERE c."BeatmapId" = b."Id"
            ), ''),
            GREATEST(b."LastUpdated", bs."LastUpdated"),
            bs."SubmittedDate",
            bs."RankedDate"
        FROM "Beatmaps" b
        JOIN "Beatmapsets" bs ON bs."Id" = b."SetId"
        WHERE b."Id" = ANY(@beatmapIds) OR b."SetId" = ANY(@setIds)
        ON CONFLICT ("Id") DO UPDATE SET
            "SetId" = EXCLUDED."SetId",
            "Mode" = EXCLUDED."Mode",
            "Status" = EXCLUDED."Status",
            "GenreId" = EXCLUDED."GenreId",
            "LanguageId" = EXCLUDED."LanguageId",
            "Nsfw" = EXCLUDED."Nsfw",
            "StarRating" = EXCLUDED."StarRating",
            "Bpm" = EXCLUDED."Bpm",
            "Cs" = EXCLUDED."Cs",
            "Ar" = EXCLUDED."Ar",
            "Od" = EXCLUDED."Od",
            "Hp" = EXCLUDED."Hp",
            "CirclesCount" = EXCLUDED."CirclesCount",
            "SlidersCount" = EXCLUDED."SlidersCount",
            "SpinnersCount" = EXCLUDED."SpinnersCount",
            "MaxCombo" = EXCLUDED."MaxCombo",
            "TotalLength" = EXCLUDED."TotalLength",
            "HitLength" = EXCLUDED."HitLength",
            "Plays" = EXCLUDED."Plays",
            "SetPlays" = EXCLUDED."SetPlays",
            "Favorites" = EXCLUDED."Favorites",
            "Rating" = EXCLUDED."Rating",
            "Version" = EXCLUDED."Version",
            "Artist" = EXCLUDED."Artist",
            "ArtistUnicode" = EXCLUDED."ArtistUnicode",
            "Title" = EXCLUDED."Title",
            "TitleUnicode" = EXCLUDED."TitleUnicode",
            "Source" = EXCLUDED."Source",
            "Tags" = EXCLUDED."Tags",
            "Description" = EXCLUDED."Description",
            "CreatorName" = EXCLUDED."CreatorName",
            "CreatorId" = EXCLUDED."CreatorId",
            "OwnerIds" = EXCLUDED."OwnerIds",
            "OwnerNames" = EXCLUDED."OwnerNames",
            "LastUpdated" = EXCLUDED."LastUpdated",
            "SubmittedDate" = EXCLUDED."SubmittedDate",
            "RankedDate" = EXCLUDED."RankedDate";
        """;
    
    private const string DeleteSql =
        """
        DELETE FROM "BeatmapSearch" s
        WHERE (s."Id" = ANY(@beatmapIds) OR s."SetId" = ANY(@setIds))
          AND NOT EXISTS (SELECT 1 FROM "Beatmaps" b WHERE b."Id" = s."Id");
        """;

    public async Task RefreshAsync(
        BanchoDbContext db,
        int[] beatmapIds,
        int[] setIds,
        CancellationToken ct
    ) {
        NpgsqlParameter[] Parameters() =>
        [
            new("beatmapIds", NpgsqlDbType.Array | NpgsqlDbType.Integer) { Value = beatmapIds },
            new("setIds", NpgsqlDbType.Array | NpgsqlDbType.Integer) { Value = setIds }
        ];

        await db.Database.ExecuteSqlRawAsync(UpsertSql, Parameters(), ct);
        await db.Database.ExecuteSqlRawAsync(DeleteSql, Parameters(), ct);
    }
}