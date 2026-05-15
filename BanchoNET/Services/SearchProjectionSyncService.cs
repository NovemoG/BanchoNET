using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BanchoNET.Services;

public class SearchProjectionSyncService(IDbContextFactory<BanchoDbContext> dbFactory) : ISearchProjectionSyncService
{
    public async Task RefreshAsync(
        int[] beatmapIds,
        int[] setIds,
        CancellationToken ct
    ) {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        
        var query = db.Beatmaps
            .AsNoTracking()
            .Include(x => x.Beatmapset)
            .AsQueryable();
        
        if (beatmapIds.Length > 0)
            query = query.Where(x => beatmapIds.AsEnumerable().Contains(x.Id));

        if (setIds.Length > 0)
            query = query.Where(x => setIds.AsEnumerable().Contains(x.SetId));
        
        var rows = await query
            .Select(x => new BeatmapSearchRow
            {
                Id = x.Id,
                SetId = x.SetId,
                Mode = x.Mode,
                Status = x.Status,
                GenreId = x.Beatmapset.GenreId,
                LanguageId = x.Beatmapset.LanguageId,
                Nsfw = x.Beatmapset.IsPrivateUpload,
                StarRating = x.StarRating,
                Bpm = x.Beatmapset.Bpm != 0 ? x.Beatmapset.Bpm : x.Bpm,
                Cs = x.Cs,
                Ar = x.Ar,
                Od = x.Od,
                Hp = x.Hp,
                CirclesCount = x.CirclesCount,
                SlidersCount = x.SlidersCount,
                SpinnersCount = x.SpinnersCount,
                MaxCombo = x.MaxCombo,
                TotalLength = x.TotalLength,
                HitLength = x.HitLength,
                Plays = x.Plays,
                Favorites = x.Beatmapset.FavoriteCount,
                Rating = (float)x.Beatmapset.Ratings.Average(),
                Version = x.Version,
                Artist = x.Beatmapset.Artist,
                ArtistUnicode = x.Beatmapset.ArtistUnicode,
                Title = x.Beatmapset.Title,
                TitleUnicode = x.Beatmapset.TitleUnicode,
                Source = x.Beatmapset.Source,
                Tags = x.Beatmapset.Tags,
                Description = x.Beatmapset.Description,
                CreatorName = x.Beatmapset.CreatorName,
                LastUpdated = x.LastUpdated > x.Beatmapset.LastUpdated ? x.LastUpdated : x.Beatmapset.LastUpdated,
                SubmittedDate = x.Beatmapset.SubmittedDate,
                RankedDate = x.Beatmapset.RankedDate
            })
            .ToListAsync(ct);
        
        foreach (var row in rows)
            await UpsertRowAsync(db, row, ct);
        
        if (setIds.Length > 0)
        {
            var beatmapsInChangedSets = await db.Beatmaps
                .AsNoTracking()
                .Where(x => setIds.AsEnumerable().Contains(x.SetId))
                .Select(x => x.Id)
                .ToListAsync(ct);

            var ids = beatmapsInChangedSets
                .Except(beatmapIds)
                .ToArray();

            if (ids.Length > 0)
            {
                var extraRows = await db.Beatmaps
                    .AsNoTracking()
                    .Include(x => x.Beatmapset)
                    .Where(x => ids.AsEnumerable().Contains(x.Id))
                    .Select(x => new BeatmapSearchRow
                    {
                        Id = x.Id,
                        SetId = x.SetId,
                        Mode = x.Mode,
                        Status = x.Status,
                        GenreId = x.Beatmapset.GenreId,
                        LanguageId = x.Beatmapset.LanguageId,
                        Nsfw = x.Beatmapset.IsPrivateUpload,
                        StarRating = x.StarRating,
                        Bpm = x.Beatmapset.Bpm != 0 ? x.Beatmapset.Bpm : x.Bpm,
                        Cs = x.Cs,
                        Ar = x.Ar,
                        Od = x.Od,
                        Hp = x.Hp,
                        CirclesCount = x.CirclesCount,
                        SlidersCount = x.SlidersCount,
                        SpinnersCount = x.SpinnersCount,
                        MaxCombo = x.MaxCombo,
                        TotalLength = x.TotalLength,
                        HitLength = x.HitLength,
                        Plays = x.Plays,
                        Favorites = x.Beatmapset.FavoriteCount,
                        Rating = (float)x.Beatmapset.Ratings.Average(),
                        Version = x.Version,
                        Artist = x.Beatmapset.Artist,
                        ArtistUnicode = x.Beatmapset.ArtistUnicode,
                        Title = x.Beatmapset.Title,
                        TitleUnicode = x.Beatmapset.TitleUnicode,
                        Source = x.Beatmapset.Source,
                        Tags = x.Beatmapset.Tags,
                        Description = x.Beatmapset.Description,
                        CreatorName = x.Beatmapset.CreatorName,
                        LastUpdated = x.LastUpdated > x.Beatmapset.LastUpdated ? x.LastUpdated : x.Beatmapset.LastUpdated,
                        SubmittedDate = x.Beatmapset.SubmittedDate,
                        RankedDate = x.Beatmapset.RankedDate
                    })
                    .ToListAsync(ct);

                foreach (var row in extraRows)
                    await UpsertRowAsync(db, row, ct);
            }
        }
    }
    
    private static async Task UpsertRowAsync(
        BanchoDbContext db,
        BeatmapSearchRow row,
        CancellationToken ct
    ) {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
INSERT INTO "BeatmapSearch"
("Id", "SetId", "Mode", "Status", "GenreId", "LanguageId", "Nsfw",
 "StarRating", "Bpm", "Cs", "Ar", "Od", "Hp", "CirclesCount", "SlidersCount", "SpinnersCount",
 "MaxCombo", "TotalLength", "HitLength", "Plays", "Favorites", "Rating", "Version", "Artist",
 "ArtistUnicode", "Title", "TitleUnicode", "Source", "Tags", "Description", "CreatorName",
 "LastUpdated", "SubmittedDate", "RankedDate")
VALUES
({row.Id}, {row.SetId}, {row.Mode}, {row.Status}, {row.GenreId}, {row.LanguageId}, {row.Nsfw},
 {row.StarRating}, {row.Bpm}, {row.Cs}, {row.Ar}, {row.Od}, {row.Hp}, {row.CirclesCount}, {row.SlidersCount},
 {row.SpinnersCount}, {row.MaxCombo}, {row.TotalLength}, {row.HitLength}, {row.Plays}, {row.Favorites},
 {row.Rating}, {row.Version}, {row.Artist}, {row.ArtistUnicode}, {row.Title}, {row.TitleUnicode}, {row.Source},
 {row.Tags}, {row.Description}, {row.CreatorName}, {row.LastUpdated}, {row.SubmittedDate}, {row.RankedDate})
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
    "LastUpdated" = EXCLUDED."LastUpdated",
    "SubmittedDate" = EXCLUDED."SubmittedDate",
    "RankedDate" = EXCLUDED."RankedDate";
""", ct);
    }
}