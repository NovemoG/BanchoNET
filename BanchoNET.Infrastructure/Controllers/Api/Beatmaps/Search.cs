using System.Globalization;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpGet("~/api/v2/beatmapsets/search")]
    public async Task<ActionResult<BeatmapsetSearchResponse>> SearchBeatmapsets(
        [FromQuery(Name = "q")] string? query,
        [FromQuery(Name = "m")] string? mode,
        [FromQuery(Name = "c")] string? category,
        [FromQuery(Name = "s")] string? status,
        [FromQuery(Name = "g")] string? genre,
        [FromQuery(Name = "l")] string? language,
        [FromQuery(Name = "e")] string? extra,
        [FromQuery(Name = "r")] string? rankAchieved,
        [FromQuery] string? sort,
        [FromQuery] string? played,
        [FromQuery] bool? nsfw,
        [FromQuery(Name = "cursor")] Dictionary<string, string>? cursor
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        cursor = cursor is { Count: 0 } ? null : cursor;
        
        var (beatmapsets, total) = await beatmapSearch.SearchAsync(
            query, mode, category, status, genre, language, extra, rankAchieved, sort, played, nsfw, cursor
        );
        
        return JsonSnake(new BeatmapsetSearchResponse
        {
            Beatmapsets = beatmapsets,
            Search = new BeatmapsetSearch
            {
                Sort = sort ?? "relevance_desc",
            },
            Cursor = BuildCursor(beatmapsets.LastOrDefault(), sort),
            Total = total
        });
    }

    private static Dictionary<string, string> BuildCursor(
        ApiBeatmapsetFull? last,
        string? sort
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

            default: //TODO
                cursor["_score"] = last?.Rating.ToString(CultureInfo.InvariantCulture) ?? "0";
                break;
        }

        return cursor;
    }
}