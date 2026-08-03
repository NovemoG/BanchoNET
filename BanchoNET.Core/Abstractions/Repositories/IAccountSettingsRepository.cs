using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IAccountSettingsRepository
{
    Task<PlayerProfileCustomizationDto> GetOrCreateCustomization(int playerId);
    Task<List<PlayerNotificationOptionDto>> GetNotificationOptions(int playerId);

    Task UpdateProfileFields(int playerId, ProfileFieldsUpdate update);
    Task UpdateCustomization(int playerId, CustomizationUpdate update);
    Task UpsertNotificationOptions(int playerId, IReadOnlyList<(string Name, string DetailsJson)> options);

    Task SetAvatar(int playerId, string extension, DateTime at);
    Task SetCover(int playerId, int? presetId, string? file, DateTime at);
}

public sealed class ProfileFieldsUpdate
{
    public string? UserFrom { get; init; }
    public string? UserInterests { get; init; }
    public string? UserOcc { get; init; }
    public string? UserTwitter { get; init; }
    public string? UserDiscord { get; init; }
    public string? UserWebsite { get; init; }
    public string? UserSig { get; init; }
    public bool? UserNotify { get; init; }
    public bool? PmFriendsOnly { get; init; }
    public bool? HidePresence { get; init; }
    public Playstyle? PlayStyle { get; init; }
}

public sealed class CustomizationUpdate
{
    public BeatmapsetDownloadType? BeatmapsetDownload { get; init; }
    public bool? BeatmapsetShowNsfw { get; init; }
    public bool? BeatmapsetShowAnimeCover { get; init; }
    public bool? BeatmapsetTitleShowOriginal { get; init; }
}