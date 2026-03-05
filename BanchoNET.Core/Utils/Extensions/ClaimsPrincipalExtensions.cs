using System.Security.Claims;

namespace BanchoNET.Core.Utils.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(
        this ClaimsPrincipal user,
        out int userId
    ) {
        userId = 0;
        if (user.Identity?.IsAuthenticated != true) return false;
        
        var sub = user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(sub, out userId);
    }
}