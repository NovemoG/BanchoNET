using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("beatmapsets/{beatmapsetId:int}")]
    public async Task<ActionResult<ApiBeatmapset>> GetBeatmapset(
        int beatmapsetId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO
        
        return new JsonResult(new ApiBeatmapset(), SnakeCaseNamingPolicy.Options);
    }
}