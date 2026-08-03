using System.IdentityModel.Tokens.Jwt;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Account;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    [HttpPut("account/password")]
    [RequireScope(OAuthScopes.Identify)]
    [RequireTrustedClient]
    public async Task<IActionResult> UpdatePassword(
        [FromForm] UpdatePasswordRequest dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var info = await Players.GetPlayerInfo(uid);
        if (info == null) return NotFound();

        var oldHash = info.PasswordHash;

        if (!passwords.Verify(dto.CurrentPassword.CreateMD5(), oldHash))
            return FormErrorResult("user", "current_password", "is incorrect");

        if (string.Equals(dto.Password, info.Username, StringComparison.OrdinalIgnoreCase))
            return FormErrorResult("user", "password", "must not be your username");

        if (string.Equals(dto.Password, info.Email, StringComparison.OrdinalIgnoreCase))
            return FormErrorResult("user", "password", "must not be your email address");

        await Players.UpdatePlayerPassword(uid, passwords.Hash(dto.Password.CreateMD5()));
        
        passwords.Invalidate(oldHash);
        
        var currentFamily = await auth.GetFamilyForJti(User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value);
        await auth.RevokeAllSessions(uid, currentFamily);
        await settings.EndClientSessions(uid);

        return await SettingsResult(uid);
    }
}