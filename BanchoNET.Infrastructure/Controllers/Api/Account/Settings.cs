using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    [HttpGet("settings")]
    [RequireScope(OAuthScopes.Identify)]
    public async Task<IActionResult> GetSettings()
    {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        return await SettingsResult(uid);
    }
}