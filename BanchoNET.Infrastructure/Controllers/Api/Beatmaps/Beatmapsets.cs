using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("beatmapsets/{beatmapsetId:int}")]
    public async Task<ActionResult<ApiBeatmapset>> GetBeatmapset(
        int beatmapsetId
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        var beatmapset = await Beatmaps.GetBeatmapSet(beatmapsetId);
        if (beatmapset == null) return NotFound();

        return JsonSnake(new ApiBeatmapset(beatmapset));
    }
    
    [HttpGet("beatmapsets/search")]
    public async Task<ActionResult> SearchBeatmapsets(
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
        
        var beatmapsets = (await Beatmaps.GetRandomBeatmaps())
            .Select(bs => new ApiBeatmapset(bs, bs.Beatmaps.First()))
            .ToList();
        
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