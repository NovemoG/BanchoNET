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
}