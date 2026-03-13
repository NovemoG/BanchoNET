using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

[Route("api/v2/beatmaps/{beatmapId:int?}")]
public partial class BeatmapsController(
    IAuthService auth,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps,
    IScoreSubmissionQueue scoresQueue,
    IScoresRepository scores
) : ApiController(auth, players, beatmaps)
{
    [HttpGet]
    public async Task<ActionResult<List<ApiBeatmap>>> GetBeatmaps(
        [FromQuery(Name = "ids[]")] int[] beatmapIds
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();

        var beatmaps = await Beatmaps.GetBeatmaps(beatmapIds);

        //TODO
        return JsonSnake(new
        {
            beatmaps = beatmaps.Select(map => new ApiBeatmap(map, new ApiBeatmapset(map.Set!, map)))
        });
    }
}