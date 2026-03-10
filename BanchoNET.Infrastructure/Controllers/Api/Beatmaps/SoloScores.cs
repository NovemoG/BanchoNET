using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpPost("solo/scores")]
    public async Task<ActionResult<ScoreResponseDto?>> PostScore(
        int beatmapId,
        [FromForm] ScoreRequestDto dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var response = await scoresQueue.EnqueueScore(dto, uid, beatmapId);
        
        return new JsonResult(response, SnakeCaseNamingPolicy.Options);
    }
    
    [HttpPut("solo/scores/{scoreId:int}")]
    public async Task<ActionResult<ApiScore>> PutScore(
        int beatmapId,
        long scoreId,
        [FromBody] ScoreSubmitRequestDto dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var response = await scoresQueue.SubmitScore(scoreId, dto);

        return new JsonResult(new ApiScore{ Rank = "F" }, SnakeCaseNamingPolicy.Options);
    }
}