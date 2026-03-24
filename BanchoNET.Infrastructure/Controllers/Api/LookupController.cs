using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[Route("api")]
public class LookupController(
    IAuthService auth,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps,
    IBeatmapHandler beatmapHandler,
    IBeatmapService beatmapService
) : ApiController(auth, players, beatmaps)
{
    [HttpGet("v2/beatmaps/lookup")]
    public async Task<ActionResult<ApiBeatmap?>> LookupBeatmap(
        string checksum,
        string filename
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        if (beatmapService.BeatmapNeedsUpdate(checksum))
            return NotFound();
        
        var beatmap = await Beatmaps.GetBeatmap(checksum);
        if (beatmap == null)
        {
            await beatmapHandler.CheckIfMapExistsOnBanchoByFilename(filename);
            return NotFound();
        }
        
        //TODO
        return JsonSnake(new ApiBeatmap(beatmap, new ApiBeatmapset(beatmap.Set, assignBeatmapsList: false)));
    }
    
    [HttpGet("v2/users/lookup")]
    public async Task<ActionResult<List<BasicApiPlayer>>> LookupUsers(
        [FromQuery(Name = "ids[]")] int[] playerIds
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();

        var players = await Players.GetPlayers(playerIds);
        //TODO fetch global rank

        return JsonSnake(players);
    }
}