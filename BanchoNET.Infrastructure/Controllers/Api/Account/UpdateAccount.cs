using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Account;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using BanchoNET.Core.Utils;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    [HttpPut("account")]
    [RequireScope(OAuthScopes.Identify)]
    public async Task<IActionResult> UpdateAccount(
        [FromForm] UpdateAccountRequest dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        if (!string.IsNullOrEmpty(dto.UserWebsite) && !IsValidWebsite(dto.UserWebsite))
            return FormErrorResult("user", "user_website", "must be a valid http or https url");

        Playstyle? playstyle = null;
        
        if (Request.Form.Keys.Any(k => k.StartsWith("user[osu_playstyle]", StringComparison.Ordinal)))
        {
            var names = (dto.Playstyle ?? []).Where(v => !string.IsNullOrWhiteSpace(v));

            if (!PlaystyleExtensions.TryParseNames(names, out var parsed))
                return FormErrorResult("user", "osu_playstyle", "contains an unknown playstyle");

            playstyle = parsed;
        }

        await settings.UpdateProfile(uid, new ProfileFieldsUpdate
        {
            UserFrom = dto.UserFrom,
            UserInterests = dto.UserInterests,
            UserOcc = dto.UserOcc,
            UserTwitter = dto.UserTwitter == null ? null : dto.UserTwitter.TrimStart('@'),
            UserDiscord = dto.UserDiscord,
            UserWebsite = dto.UserWebsite,
            UserSig = dto.UserSig,
            UserNotify = RequestValues.ParseBool(dto.UserNotify),
            PmFriendsOnly = RequestValues.ParseBool(dto.PmFriendsOnly),
            HidePresence = RequestValues.ParseBool(dto.HidePresence),
            PlayStyle = playstyle
        });

        return await SettingsResult(uid);
    }

    private static bool IsValidWebsite(
        string value
    ) {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}