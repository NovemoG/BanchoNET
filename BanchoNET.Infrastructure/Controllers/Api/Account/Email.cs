using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Account;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    [HttpPut("account/email")]
    [RequireScope(OAuthScopes.Identify)]
    [RequireTrustedClient]
    public async Task<IActionResult> UpdateEmail(
        [FromForm] UpdateEmailRequest dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var info = await Players.GetPlayerInfo(uid);
        if (info == null) return NotFound();

        if (!passwords.Verify(dto.CurrentPassword.CreateMD5(), info.PasswordHash))
            return FormErrorResult("user", "current_password", "is incorrect");

        var email = dto.Email.Trim().ToLowerInvariant();

        if (email != info.Email && await Players.EmailTaken(email))
            return FormErrorResult("user", "user_email", "is already taken");

        //TODO confirmation link and notify the previous address
        await Players.UpdatePlayerEmail(uid, email);
        
        return await SettingsResult(uid);
    }
}