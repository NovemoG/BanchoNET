using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Core.Models.Api.Account;

public sealed class UpdateOptionsRequest
{
    [FromForm(Name = "user_profile_customization[beatmapset_download]")]
    [KeepEmptyString]
    public string? BeatmapsetDownload { get; set; }

    [FromForm(Name = "user_profile_customization[beatmapset_show_nsfw]")]
    [KeepEmptyString]
    public string? BeatmapsetShowNsfw { get; set; }

    [FromForm(Name = "user_profile_customization[beatmapset_show_anime_cover]")]
    [KeepEmptyString]
    public string? BeatmapsetShowAnimeCover { get; set; }

    [FromForm(Name = "user_profile_customization[beatmapset_title_show_original]")]
    [KeepEmptyString]
    public string? BeatmapsetTitleShowOriginal { get; set; }
}