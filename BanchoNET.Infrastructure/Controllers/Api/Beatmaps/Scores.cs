using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpGet("scores")]
    public async Task<ActionResult<BeatmapScoresResponseDto>> GetScores(
        int beatmapId,
        [FromQuery] string type,
        [FromQuery] string mode,
        [FromQuery] int limit
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO

        return new JsonResult(new BeatmapScoresResponseDto(), SnakeCaseNamingPolicy.Options);
    }
}