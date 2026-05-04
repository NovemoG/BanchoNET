using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("beatmapsets/{beatmapsetId:int}")]
    public async Task<ActionResult<ApiBeatmapsetFull>> GetBeatmapset(
        int beatmapsetId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var beatmapset = await Beatmaps.GetBeatmapsetFromApiOrCached(beatmapsetId, withAllData: false);
        if (beatmapset == null) return NotFound();
        
        await Beatmaps.FetchPlayerPlaycount(beatmapset, uid);
        await Beatmaps.FetchPlayerFavorited(beatmapset, uid);

        return JsonSnake(beatmapset);
    }
    
    [HttpGet("beatmapsets/search")]
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

        var beatmapsets = await Beatmaps.GetRandomBeatmaps();
        
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