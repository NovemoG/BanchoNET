using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Account;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using BanchoNET.Core.Utils;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    private const string CustomizationGroup = "user_profile_customization";

    [HttpPut("account/options")]
    [RequireScope(OAuthScopes.Identify)]
    public async Task<IActionResult> UpdateOptions(
        [FromForm] UpdateOptionsRequest dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        BeatmapsetDownloadType? download = null;

        if (dto.BeatmapsetDownload != null)
        {
            if (!AccountSettingsService.TryParseDownloadType(dto.BeatmapsetDownload, out var parsed))
                return FormErrorResult(CustomizationGroup, "beatmapset_download", "is not a valid download type");
            
            if (parsed == BeatmapsetDownloadType.Direct)
            {
                var info = await Players.GetPlayerInfo(uid);

                if (info?.IsSupporter != true)
                    return FormErrorResult(CustomizationGroup, "beatmapset_download", "requires supporter");
            }

            download = parsed;
        }

        await settings.UpdateCustomization(uid, new CustomizationUpdate
        {
            BeatmapsetDownload = download,
            BeatmapsetShowNsfw = RequestValues.ParseBool(dto.BeatmapsetShowNsfw),
            BeatmapsetShowAnimeCover = RequestValues.ParseBool(dto.BeatmapsetShowAnimeCover),
            BeatmapsetTitleShowOriginal = RequestValues.ParseBool(dto.BeatmapsetTitleShowOriginal)
        });

        return await SettingsResult(uid);
    }
}