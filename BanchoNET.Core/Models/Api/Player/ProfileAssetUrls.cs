using BanchoNET.Core.Utils;

namespace BanchoNET.Core.Models.Api.Player;

public static class ProfileAssetUrls
{
    public const int PresetCoverCount = 8;
    public const int DefaultCoverPresetId = 1;

    public static bool IsKnownPreset(
        int presetId
    ) => presetId is >= 1 and <= PresetCoverCount;

    public static string Avatar(
        int playerId,
        DateTime? updatedAt
    ) {
        var url = $"https://a.{AppSettings.Domain}/{playerId}";

        return updatedAt == null
            ? url
            : $"{url}?{ToUnix(updatedAt.Value)}";
    }

    public static Cover Cover(
        int playerId,
        int? presetId,
        string? coverFile,
        DateTime? updatedAt
    ) {
        var customUrl = coverFile == null
            ? null
            : $"https://assets.{AppSettings.Domain}/user-profile-covers/{coverFile}?{ToUnix(updatedAt ?? DateTime.UtcNow)}";

        var effectivePreset = presetId ?? (customUrl == null ? DefaultCoverPresetId : (int?)null);

        return new Cover
        {
            Id = effectivePreset?.ToString(),
            CustomUrl = customUrl,
            Url = effectivePreset != null
                ? $"https://assets.{AppSettings.Domain}/user-profile-covers/{effectivePreset}.jpg"
                : customUrl!
        };
    }

    private static long ToUnix(
        DateTime value
    ) => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)).ToUnixTimeSeconds();
}