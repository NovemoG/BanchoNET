using System.Text.Json.Nodes;
using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api.Account;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Infrastructure.Services;

public class AccountSettingsService(
    IPlayersRepository players,
    IAccountSettingsRepository settings,
    IPlayerService playerService,
    IPlayerCoordinator playerCoordinator,
    ILazerPlayerService lazerPlayers
) : IAccountSettingsService
{
    public async Task EndClientSessions(
        int playerId
    ) {
        var live = playerService.GetPlayer(playerId);
        if (live != null) await playerCoordinator.LogoutPlayer(live);
        
        await lazerPlayers.RemovePlayer(playerId);
    }

    public async Task<SettingsResponse?> GetSettings(
        int playerId
    ) {
        var response = await players.GetFullPlayerInfo<SettingsResponse>(playerId);
        if (response == null) return null;
        
        var info = await players.GetPlayerInfoWithCustomization(playerId);
        if (info == null) return null;

        var customization = info.ProfileCustomization ?? await settings.GetOrCreateCustomization(playerId);
        var notificationOptions = await settings.GetNotificationOptions(playerId);

        response.SessionVerified = true;
        response.SessionVerificationMethod = null; //TODO totp
        response.UserSig = info.UserSig;
        response.UserNotify = info.UserNotify;
        response.HidePresence = info.HideOnlineActivity;

        response.UserProfileCustomization = new ProfileCustomizationResponse
        {
            BeatmapsetDownload = ToWire(customization.BeatmapsetDownload),
            BeatmapsetShowNsfw = customization.BeatmapsetShowNsfw,
            BeatmapsetShowAnimeCover = customization.BeatmapsetShowAnimeCover,
            BeatmapsetTitleShowOriginal = customization.BeatmapsetTitleShowOriginal
        };

        response.UserNotificationOptions = BuildNotificationOptions(notificationOptions);

        return response;
    }

    public async Task UpdateProfile(
        int playerId,
        ProfileFieldsUpdate update
    ) {
        await settings.UpdateProfileFields(playerId, update);
        
        var live = playerService.GetPlayer(playerId);
        if (live != null)
        {
            if (update.PmFriendsOnly != null) live.PmFriendsOnly = update.PmFriendsOnly.Value;
            if (update.HidePresence != null) live.AppearOffline = update.HidePresence.Value;
        }

        await RefreshLazerCache(playerId);
    }

    public async Task UpdateCustomization(
        int playerId,
        CustomizationUpdate update
    ) {
        await settings.UpdateCustomization(playerId, update);
        await RefreshLazerCache(playerId);
    }

    public async Task UpdateNotificationOptions(
        int playerId,
        IReadOnlyList<(string Name, string DetailsJson)> options
    ) {
        await settings.UpsertNotificationOptions(playerId, options);
    }

    public async Task SetAvatar(
        int playerId,
        string extension
    ) {
        await settings.SetAvatar(playerId, extension, DateTime.UtcNow);
        await RefreshLazerCache(playerId);
    }

    public async Task SetCover(
        int playerId,
        int? presetId,
        string? file
    ) {
        await settings.SetCover(playerId, presetId, file, DateTime.UtcNow);
        await RefreshLazerCache(playerId);
    }
    
    private async Task RefreshLazerCache(
        int playerId
    ) {
        if (!await lazerPlayers.IsOnline(playerId)) return;

        var fresh = await players.GetPlayerInfoForMode<ApiPlayer>(playerId);
        if (fresh == null) return;

        await lazerPlayers.RefreshPlayer(playerId, fresh);
    }

    private static NotificationOptionResponse[] BuildNotificationOptions(
        List<PlayerNotificationOptionDto> stored
    ) {
        var byName = stored.ToDictionary(o => o.Name);
        var result = new List<NotificationOptionResponse>();
        
        foreach (var name in NotificationOptionNames.DeliveryRows)
        {
            if (byName.ContainsKey(name)) continue;

            result.Add(new NotificationOptionResponse
            {
                Id = 0,
                Name = name,
                Details = new JsonObject
                {
                    [NotificationOptionNames.MailKey] = NotificationOptionNames.DefaultMail(name),
                    [NotificationOptionNames.PushKey] = NotificationOptionNames.DefaultPush(name)
                }
            });
        }

        foreach (var option in stored)
        {
            result.Add(new NotificationOptionResponse
            {
                Id = option.Id,
                Name = option.Name,
                Details = ParseDetails(option.Details)
            });
        }

        return result.OrderBy(o => o.Name, StringComparer.Ordinal).ToArray();
    }

    private static JsonNode? ParseDetails(
        string details
    ) {
        try
        {
            return JsonNode.Parse(details);
        }
        catch (System.Text.Json.JsonException)
        {
            return new JsonObject();
        }
    }

    public static string ToWire(
        BeatmapsetDownloadType type
    ) => type switch
    {
        BeatmapsetDownloadType.NoVideo => "no_video",
        BeatmapsetDownloadType.Direct => "direct",
        _ => "all"
    };

    public static bool TryParseDownloadType(
        string value,
        out BeatmapsetDownloadType type
    ) {
        switch (value)
        {
            case "all": type = BeatmapsetDownloadType.All; return true;
            case "no_video": type = BeatmapsetDownloadType.NoVideo; return true;
            case "direct": type = BeatmapsetDownloadType.Direct; return true;
            default: type = BeatmapsetDownloadType.All; return false;
        }
    }
}