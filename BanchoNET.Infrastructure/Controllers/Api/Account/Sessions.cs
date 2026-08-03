using System.IdentityModel.Tokens.Jwt;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    [HttpGet("account/sessions")]
    [RequireScope(OAuthScopes.Identify)]
    [RequireTrustedClient]
    public async Task<IActionResult> GetSessions()
    {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        return JsonSnake(new { sessions = await auth.GetSessions(uid, jti) });
    }

    [HttpDelete("account/sessions/{id:guid}")]
    [RequireScope(OAuthScopes.Identify)]
    [RequireTrustedClient]
    public async Task<IActionResult> DeleteSession(
        Guid id
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        // 404 when the family belongs to someone else, so this cannot be used to
        // probe for the existence of another user's sessions.
        return await auth.RevokeSession(uid, id) ? NoContent() : NotFound();
    }
}