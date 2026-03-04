using BanchoNET.Core.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("me/beatmapset-favourites")]
    public ActionResult<BeatmapsetFavorites> GetBeatmapsetFavourites() {
        if (!TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get favorites

        return Ok(Array.Empty<int>());
    }
}