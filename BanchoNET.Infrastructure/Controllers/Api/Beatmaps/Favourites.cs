using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpPost("~/api/v2/beatmapsets/{beatmapsetId:int}/favourites")]
    public async Task<ActionResult> PostFavourite(
        int beatmapsetId,
        [FromForm] string action
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        FavoriteAction favAction;
        switch (action.ToLower())
        {
            case "favourite":
                favAction = FavoriteAction.Add;
                break;
            case "unfavourite":
                favAction = FavoriteAction.Remove;
                break;
            default:
                return BadRequest();
        }
        
        var beatmapset = await Beatmaps.GetBeatmapset(beatmapsetId);
        if (beatmapset == null) return NotFound();
        
        var add = favAction == FavoriteAction.Add;
        beatmapset.FavoriteCount += add ? 1 : -1;
        
        await beatmapsRepository.UpdateBeatmapsetFavoriteCount(beatmapset, uid, add);

        return Ok(new { favourite_count = beatmapset.FavoriteCount });
    }
    
    private enum FavoriteAction
    {
        Add,
        Remove
    }
}