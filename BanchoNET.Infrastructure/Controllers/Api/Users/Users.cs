using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

[Route("api/v2/users/{userId:int}")]
public partial class UsersController(
    IAuthService auth,
    IPlayersRepository players
) : ApiController(auth, players)
{
    [HttpGet("{forMode?}")]
    public async Task<ActionResult<ApiPlayer?>> GetUsers(
        int userId,
        string? forMode = null,
        [FromQuery] string key = "id"
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();
        
        var mode = GameMode.RelaxStd;
        if (!string.IsNullOrWhiteSpace(forMode))
            if (!EnumExtensions.ToModeMap.TryGetValue(forMode, out mode))
                return BadRequest();

        var apiPlayer = await Players.GetPlayerInfoForMode<ApiPlayer>(userId, mode);
        if (apiPlayer == null) return NotFound();
        
        return new JsonResult(apiPlayer, SnakeCaseNamingPolicy.ApiPlayerOptions);
    }
}