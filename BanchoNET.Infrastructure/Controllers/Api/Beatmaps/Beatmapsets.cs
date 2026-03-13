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
}