using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

[Route("api/v2/beatmaps/{beatmapId:int?}")]
public partial class BeatmapsController(
    IAuthService auth,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps,
    IScoreSubmissionQueue scoresQueue
) : ApiController(auth, players, beatmaps)
{
    [HttpGet]
    public async Task<ActionResult<ApiBeatmap[]>> GetBeatmaps(
        [FromQuery(Name = "ids[]")] int[] beatmapIds
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO

        return new JsonResult(Array.Empty<ApiBeatmap>(), SnakeCaseNamingPolicy.Options);
    }
    
    [HttpGet("lookup")]
    public async Task<ActionResult<ApiBeatmap>> LookupBeatmap(
        string checksum,
        string filename
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO

        return new JsonResult(Array.Empty<ApiBeatmap>(), SnakeCaseNamingPolicy.Options);
    }
}