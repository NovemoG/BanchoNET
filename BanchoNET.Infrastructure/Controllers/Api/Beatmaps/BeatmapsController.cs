using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

[Route("api/v2/beatmaps")]
public partial class BeatmapsController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IBeatmapsRepository beatmapsRepository,
    IBeatmapService beatmapService,
    IScoreSubmissionQueue scoresQueue,
    ILazerScoresRepository scores
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpGet]
    [AllowClientCredentials]
    public async Task<ActionResult<List<ApiBeatmap>>> GetBeatmaps(
        [FromQuery(Name = "ids[]")] int[] beatmapIds
    ) {
        var beatmaps = await Beatmaps.GetBeatmaps(beatmapIds);

        //TODO
        return JsonSnake(new
        {
            beatmaps = beatmaps.Select(map => new ApiBeatmap(map, new ApiBeatmapset(map.Set)))
        });
    }

    [HttpGet("{beatmapId:int}")]
    [AllowClientCredentials]
    public async Task<ActionResult<ApiBeatmap?>> GetBeatmap(
        int beatmapId
    ) {
        var beatmap = await Beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null) return NotFound();

        return JsonSnake(new ApiBeatmap(beatmap, new ApiBeatmapset(beatmap.Set)));
    }

    [HttpGet("lookup")]
    [AllowClientCredentials]
    public async Task<ActionResult<ApiBeatmap?>> LookupBeatmap(
        string checksum,
        string? filename
    ) {
        if (beatmapService.BeatmapNeedsUpdate(checksum))
            return NotFound();

        var beatmap = await Beatmaps.GetBeatmap(checksum);
        if (beatmap == null)
        {
            if (!string.IsNullOrWhiteSpace(filename))
                await Beatmaps.CheckIfMapExistsOnBanchoByFilename(filename);

            return NotFound();
        }

        return JsonSnake(new ApiBeatmap(beatmap, new ApiBeatmapset(beatmap.Set)));
    }
}