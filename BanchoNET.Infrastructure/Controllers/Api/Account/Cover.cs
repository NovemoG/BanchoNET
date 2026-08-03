using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    [HttpPost("account/cover")]
    [RequireScope(OAuthScopes.Identify)]
    [RequestSizeLimit(ProfileImageService.CoverMaxBytes)]
    public async Task<IActionResult> UploadCover(
        [FromForm(Name = "cover_file")] IFormFile? coverFile,
        [FromForm(Name = "cover_id")] string? coverId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var hasFile = coverFile != null;
        var hasPreset = !string.IsNullOrWhiteSpace(coverId);

        if (hasFile == hasPreset)
            return FormErrorResult("user", "cover_file", "requires exactly one of cover_file or cover_id");

        var info = await Players.GetPlayerInfo(uid);
        if (info == null) return NotFound();

        if (hasPreset)
        {
            if (!int.TryParse(coverId, out var presetId) || !ProfileAssetUrls.IsKnownPreset(presetId))
                return FormErrorResult("user", "cover_id", "is not a known cover");
            
            await settings.SetCover(uid, presetId, info.CoverFile);

            return await SettingsResult(uid);
        }

        if (!info.IsSupporter)
            return FormErrorResult("user", "cover_file", "requires supporter");

        var result = await images.Store(uid, coverFile!, ProfileImageKind.Cover);

        if (!result.Success)
            return FormErrorResult("user", "cover_file", result.Error!);
        
        await settings.SetCover(uid, null, $"{uid}.{result.Extension}");

        return await SettingsResult(uid);
    }
}