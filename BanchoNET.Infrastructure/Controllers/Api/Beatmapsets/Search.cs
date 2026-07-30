using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmapsets;

public partial class BeatmapsetsController
{
    [HttpGet("search")]
    [AllowClientCredentials]
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
        cursor = cursor is { Count: 0 } ? null : cursor;
        
        var (beatmapsets, total, nextCursor) = await beatmapSearch.SearchAsync(
            query, mode, category, status, genre, language, extra, rankAchieved, sort, played, nsfw, cursor
        );
        
        return JsonSnake(new BeatmapsetSearchResponse
        {
            Beatmapsets = beatmapsets,
            Search = new BeatmapsetSearch
            {
                Sort = sort ?? "relevance_desc",
            },
            Cursor = nextCursor,
            Total = total
        });
    }
}