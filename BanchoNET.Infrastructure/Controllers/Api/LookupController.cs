using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[Route("api")]
public class LookupController(
    IAuthService auth,
    IPlayersRepository players,
    ILazerPlayerService playerService,
    IBeatmapHandler beatmaps,
    IBeatmapService beatmapService
) : ApiController(auth, players, playerService, beatmaps)
{
    [HttpGet("v2/beatmaps/lookup")]
    public async Task<ActionResult<ApiBeatmap?>> LookupBeatmap(
        string checksum,
        string? filename
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
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

    [HttpGet("v2/beatmapsets/lookup")]
    public async Task<ActionResult<ApiBeatmapsetFull?>> LookupBeatmapset(
        [FromQuery(Name = "beatmap_id")] int beatmapId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var beatmapset = await Beatmaps.GetBeatmapsetByMapIdFromApiOrCached(beatmapId);
        if (beatmapset == null) return NotFound();
        
        await Beatmaps.FetchPlayerPlaycount(beatmapset, uid);
        await Beatmaps.FetchPlayerFavorited(beatmapset, uid);
        
        return JsonSnake(beatmapset);
    }
    
    [HttpGet("v2/users/lookup")]
    public async Task<ActionResult> LookupUsers(
        [FromQuery(Name = "ids[]")] int[] playerIds
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();

        var players = await Players.GetPlayers(playerIds);

        return JsonSnake(new { users = players });
    }
}