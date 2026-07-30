using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmapsets;

[Route("api/v2/beatmapsets")]
public partial class BeatmapsetsController(
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IBeatmapsRepository beatmapsRepository,
    IBeatmapDownloader beatmapDownloader,
    IBeatmapSearchService beatmapSearch,
    ILazerScoresRepository scores
) : ApiControllerBase(players, playerService, beatmaps)
{
    [HttpGet("{beatmapsetId:int}")]
    [AllowClientCredentials]
    public async Task<ActionResult<ApiBeatmapsetFull>> GetBeatmapset(
        int beatmapsetId
    ) {
        var beatmapset = await Beatmaps.GetBeatmapsetFromApiOrCached(beatmapsetId, withAllData: false);
        if (beatmapset == null) return NotFound();
        
        if (User.TryGetUserId(out var uid))
        {
            await Beatmaps.FetchPlayerPlaycount(beatmapset, uid);
            await Beatmaps.FetchPlayerFavorited(beatmapset, uid);
        }

        return JsonSnake(beatmapset);
    }

    [HttpGet("lookup")]
    [AllowClientCredentials]
    public async Task<ActionResult<ApiBeatmapsetFull?>> LookupBeatmapset(
        [FromQuery(Name = "beatmap_id")] int beatmapId
    ) {
        var beatmapset = await Beatmaps.GetBeatmapsetByMapIdFromApiOrCached(beatmapId);
        if (beatmapset == null) return NotFound();

        if (User.TryGetUserId(out var uid))
        {
            await Beatmaps.FetchPlayerPlaycount(beatmapset, uid);
            await Beatmaps.FetchPlayerFavorited(beatmapset, uid);
        }

        return JsonSnake(beatmapset);
    }
}