using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("me/beatmapset-favourites")]
    [RequireScope(OAuthScopes.Identify)]
    public ActionResult<BeatmapsetFavorites> GetBeatmapsetFavourites() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get favorites

        return JsonSnake(new BeatmapsetFavorites());
    }
}