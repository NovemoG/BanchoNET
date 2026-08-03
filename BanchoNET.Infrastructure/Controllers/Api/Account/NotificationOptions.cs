using System.Text.Json;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Account;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Account;

public partial class AccountController
{
    private const string NotificationGroup = "user_notification_options";

    [HttpPut("account/notification-options")]
    [RequireScope(OAuthScopes.Identify)]
    public async Task<IActionResult> UpdateNotificationOptions(
        [ModelBinder(typeof(NotificationOptionsModelBinder))] List<NotificationOptionInput> dto
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var updates = new List<(string Name, string DetailsJson)>();

        foreach (var option in dto)
        {
            if (!NotificationOptionNames.IsKnown(option.Name))
                return FormErrorResult(NotificationGroup, option.Name, "is not a known notification");

            foreach (var (key, value) in option.Details)
            {
                if (!NotificationOptionNames.IsKeyAllowed(option.Name, key))
                    return FormErrorResult(NotificationGroup, option.Name, $"does not accept \"{key}\"");

                if (value is string[] list && !NotificationOptionNames.AreListValuesValid(key, list))
                    return FormErrorResult(NotificationGroup, option.Name, $"has an invalid value in \"{key}\"");
            }

            updates.Add((option.Name, JsonSerializer.Serialize(option.Details)));
        }
        
        await settings.UpdateNotificationOptions(uid, updates);

        return await SettingsResult(uid);
    }
}