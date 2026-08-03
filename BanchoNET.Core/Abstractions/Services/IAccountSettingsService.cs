using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Api.Player;

namespace BanchoNET.Core.Abstractions.Services;

public interface IAccountSettingsService
{
    Task<SettingsResponse?> GetSettings(int playerId);

    Task UpdateProfile(int playerId, ProfileFieldsUpdate update);
    Task UpdateCustomization(int playerId, CustomizationUpdate update);
    Task UpdateNotificationOptions(int playerId, IReadOnlyList<(string Name, string DetailsJson)> options);

    Task SetAvatar(int playerId, string extension);
    Task SetCover(int playerId, int? presetId, string? file);
    
    Task EndClientSessions(int playerId);
}