using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse?>> GetMe() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var apiPlayer = await Players.GetFullPlayerInfo(uid);
        if (apiPlayer == null) return NotFound();

        apiPlayer.SessionVerified = true;
        apiPlayer.SessionVerificationMethod = null; //TODO
        
        return new JsonResult(apiPlayer, SnakeCaseNamingPolicy.ApiPlayerOptions);
    }
}