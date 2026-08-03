using System.Text.Json.Nodes;

namespace BanchoNET.Core.Models.Api.Player;

public class SettingsResponse : MeResponse
{
    public string? UserSig { get; set; }

    public bool UserNotify { get; set; }
    public bool HidePresence { get; set; }

    public ProfileCustomizationResponse UserProfileCustomization { get; set; } = new();
    public NotificationOptionResponse[] UserNotificationOptions { get; set; } = [];
}

public class ProfileCustomizationResponse
{
    public string BeatmapsetDownload { get; set; } = "all";

    public bool BeatmapsetShowNsfw { get; set; }
    public bool BeatmapsetShowAnimeCover { get; set; } = true;
    public bool BeatmapsetTitleShowOriginal { get; set; }
}

public class NotificationOptionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public JsonNode? Details { get; set; }
}