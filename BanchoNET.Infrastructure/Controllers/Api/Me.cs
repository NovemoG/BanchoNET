using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiPlayer?>> GetMe() {
        var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(sub, out var uid)) return Unauthorized();
        
        var user = await db.Players.FindAsync(uid);
        if (user == null) return NotFound();

        return new JsonResult(new ApiPlayer(), SnakeCaseNamingPolicy.Options); //TODO
    }
}