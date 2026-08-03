using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    [HttpPost("account/avatar")]
    [RequireScope(OAuthScopes.Identify)]
    [RequestSizeLimit(ProfileImageService.AvatarMaxBytes)]
    public async Task<IActionResult> UploadAvatar(
        [FromForm(Name = "avatar_file")] IFormFile? avatarFile
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        if (avatarFile == null)
            return FormErrorResult("user", "avatar_file", "is required");

        var result = await images.Store(uid, avatarFile, ProfileImageKind.Avatar);

        if (!result.Success)
            return FormErrorResult("user", "avatar_file", result.Error!);

        await settings.SetAvatar(uid, result.Extension!);

        return await SettingsResult(uid);
    }
}