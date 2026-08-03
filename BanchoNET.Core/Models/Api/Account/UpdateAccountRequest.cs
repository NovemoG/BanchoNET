using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using BanchoNET.Core.Utils;

namespace BanchoNET.Core.Models.Api.Account;

[AttributeUsage(AttributeTargets.Property)]
public sealed class KeepEmptyStringAttribute : DisplayFormatAttribute
{
    public KeepEmptyStringAttribute() => ConvertEmptyStringToNull = false;
}

public sealed class UpdateAccountRequest
{
    [FromForm(Name = "user[user_from]")]
    [KeepEmptyString]
    [StringLength(100, ErrorMessage = "is too long")]
    public string? UserFrom { get; set; }

    [FromForm(Name = "user[user_interests]")]
    [KeepEmptyString]
    [StringLength(255, ErrorMessage = "is too long")]
    public string? UserInterests { get; set; }

    [FromForm(Name = "user[user_occ]")]
    [KeepEmptyString]
    [StringLength(255, ErrorMessage = "is too long")]
    public string? UserOcc { get; set; }

    [FromForm(Name = "user[user_twitter]")]
    [KeepEmptyString]
    [StringLength(255, ErrorMessage = "is too long")]
    public string? UserTwitter { get; set; }

    [FromForm(Name = "user[user_discord]")]
    [KeepEmptyString]
    [StringLength(37, ErrorMessage = "is too long")]
    public string? UserDiscord { get; set; }

    [FromForm(Name = "user[user_website]")]
    [KeepEmptyString]
    [StringLength(200, ErrorMessage = "is too long")]
    public string? UserWebsite { get; set; }

    [FromForm(Name = "user[user_sig]")]
    [KeepEmptyString]
    [StringLength(3000, ErrorMessage = "is too long")]
    public string? UserSig { get; set; }
    
    [FromForm(Name = "user[user_notify]")]
    [KeepEmptyString]
    public string? UserNotify { get; set; }

    [FromForm(Name = "user[pm_friends_only]")]
    [KeepEmptyString]
    public string? PmFriendsOnly { get; set; }

    [FromForm(Name = "user[hide_presence]")]
    [KeepEmptyString]
    public string? HidePresence { get; set; }

    [FromForm(Name = "user[osu_playstyle][]")]
    public string[]? Playstyle { get; set; }
}