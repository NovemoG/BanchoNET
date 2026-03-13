using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public class LookupController(
    IAuthService auth,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps,
    IBeatmapHandler beatmapHandler,
    IBeatmapService beatmapService
) : ApiController(auth, players, beatmaps)
{
    [HttpGet("beatmaps/lookup")]
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
            if (await beatmapHandler.CheckIfMapExistsOnBanchoByFilename(filename))
                beatmapService.InsertNeedsUpdateBeatmap(checksum);
            
            return NotFound();
        }
        
        //TODO
        return JsonSnake(new ApiBeatmap(beatmap, new ApiBeatmapset(beatmap.Set!, beatmap)));
    }
    
    [HttpGet("users/lookup")]
    public async Task<ActionResult<List<BasicApiPlayer>>> LookupUsers(
        [FromQuery(Name = "ids[]")] int[] playerIds
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();

        var players = await Players.GetPlayers(playerIds);
        //TODO fetch global rank

        return JsonSnake(players);
    }
}