using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("me")]
    [RequireScope(OAuthScopes.Identify)]
    public async Task<ActionResult<MeResponse?>> GetMe() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var apiPlayer = await Players.GetFullPlayerInfo(uid);
        if (apiPlayer == null) return NotFound();

        apiPlayer.SessionVerified = true;
        apiPlayer.SessionVerificationMethod = null; //TODO
        
        return JsonSnake(apiPlayer);
    }
    
    [HttpPut("me/profile-mods")]
    [RequireScope(OAuthScopes.Identify)]
    public async Task<IActionResult> UpdateProfileMods(
        [FromForm] string mode,
        [FromForm(Name = "mods[]")] string[] mods,
        [FromForm(Name = "match_exactly")] bool matchExactly = false
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        if (!EnumExtensions.ToModeMap.TryGetValue(mode, out var gameMode))
            return BadRequest();
        
        if (mods.Length > 16 || mods.Any(m => m.Length is < 2 or > 3 || !m.All(char.IsLetterOrDigit)))
            return BadRequest();

        var modKeys = mods.Aggregate(string.Empty, (current, mod) => $"{current}{mod};").ToUpper();
        await Players.UpdatePlayerProfileDefaultMods(uid, (byte)gameMode, modKeys, matchExactly);

        return NoContent();
    }
}