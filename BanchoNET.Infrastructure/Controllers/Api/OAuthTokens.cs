using System.IdentityModel.Tokens.Jwt;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpDelete("oauth/tokens/current")]
    [RequireScope(OAuthScopes.None)]
    // Works off the jti alone, and an app token has to be able to revoke itself.
    [AllowClientCredentials]
    public async Task<IActionResult> RevokeCurrentToken() {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti)) return NoContent();
        
        var exp = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        var expiresAt = long.TryParse(exp, out var unix)
            ? DateTimeOffset.FromUnixTimeSeconds(unix)
            : DateTimeOffset.UtcNow.AddDays(1);

        await auth.RevokeToken(jti, expiresAt);

        return NoContent();
    }
}