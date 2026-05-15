using BanchoNET.Core.Models.Api;
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
        string? sort,
        string? played,
        bool? nsfw
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        //TODO sometimes lazer nonstop requests beatmap listing
        
        var beatmapsets = await beatmapSearch.SearchAsync(
            query, mode, category, status, genre, language, extra, rankAchieved, sort, played, nsfw
        );
        
        return JsonSnake(new BeatmapsetSearchResponse
        {
            Beatmapsets = beatmapsets,
            Search = new BeatmapsetSearch
            {
                Sort = sort ?? "ranked_desc",
            },
            Total = beatmapsets.Count
        });
    }
}