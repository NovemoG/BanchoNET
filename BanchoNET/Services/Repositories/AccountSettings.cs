using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class AccountSettingsRepository(
	BanchoDbContext dbContext
) : IAccountSettingsRepository
{
	public async Task<PlayerProfileCustomizationDto> GetOrCreateCustomization(int playerId)
	{
		var customization = await dbContext.PlayerProfileCustomizations
			.FirstOrDefaultAsync(c => c.PlayerId == playerId);

		if (customization != null) return customization;
		
		customization = new PlayerProfileCustomizationDto
		{
			PlayerId = playerId,
			UpdatedAt = DateTime.UtcNow
		};

		dbContext.PlayerProfileCustomizations.Add(customization);
		await dbContext.SaveChangesAsync();

		return customization;
	}

	public async Task<List<PlayerNotificationOptionDto>> GetNotificationOptions(int playerId)
	{
		return await dbContext.PlayerNotificationOptions
			.Where(o => o.PlayerId == playerId)
			.OrderBy(o => o.Id)
			.ToListAsync();
	}

	public async Task UpdateProfileFields(int playerId, ProfileFieldsUpdate update)
	{
		var player = await dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId);
		if (player == null) return;

		if (update.UserFrom != null) player.UserFrom = Normalize(update.UserFrom);
		if (update.UserInterests != null) player.UserInterests = Normalize(update.UserInterests);
		if (update.UserOcc != null) player.UserOcc = Normalize(update.UserOcc);
		if (update.UserTwitter != null) player.UserTwitter = Normalize(update.UserTwitter);
		if (update.UserDiscord != null) player.UserDiscord = Normalize(update.UserDiscord);
		if (update.UserWebsite != null) player.UserWebsite = Normalize(update.UserWebsite);
		if (update.UserSig != null) player.UserSig = Normalize(update.UserSig);
		if (update.UserNotify != null) player.UserNotify = update.UserNotify.Value;
		if (update.PmFriendsOnly != null) player.PmFriendsOnly = update.PmFriendsOnly.Value;
		if (update.HidePresence != null) player.HideOnlineActivity = update.HidePresence.Value;
		if (update.PlayStyle != null) player.PlayStyle = update.PlayStyle.Value;

		await dbContext.SaveChangesAsync();
	}

	public async Task UpdateCustomization(int playerId, CustomizationUpdate update)
	{
		var customization = await GetOrCreateCustomization(playerId);

		if (update.BeatmapsetDownload != null) customization.BeatmapsetDownload = update.BeatmapsetDownload.Value;
		if (update.BeatmapsetShowNsfw != null) customization.BeatmapsetShowNsfw = update.BeatmapsetShowNsfw.Value;
		if (update.BeatmapsetShowAnimeCover != null) customization.BeatmapsetShowAnimeCover = update.BeatmapsetShowAnimeCover.Value;
		if (update.BeatmapsetTitleShowOriginal != null) customization.BeatmapsetTitleShowOriginal = update.BeatmapsetTitleShowOriginal.Value;

		customization.UpdatedAt = DateTime.UtcNow;

		await dbContext.SaveChangesAsync();
	}

	public async Task UpsertNotificationOptions(
		int playerId,
		IReadOnlyList<(string Name, string DetailsJson)> options
	) {
		if (options.Count == 0) return;

		var names = options.Select(o => o.Name).ToArray();
		var existing = await dbContext.PlayerNotificationOptions
			.Where(o => o.PlayerId == playerId && names.Contains(o.Name))
			.ToDictionaryAsync(o => o.Name);

		var now = DateTime.UtcNow;

		foreach (var (name, details) in options)
		{
			if (existing.TryGetValue(name, out var row))
			{
				row.Details = details;
				row.UpdatedAt = now;
				continue;
			}

			dbContext.PlayerNotificationOptions.Add(new PlayerNotificationOptionDto
			{
				PlayerId = playerId,
				Name = name,
				Details = details,
				UpdatedAt = now
			});
		}

		await dbContext.SaveChangesAsync();
	}

	public async Task SetAvatar(int playerId, string extension, DateTime at)
	{
		await dbContext.Players
			.Where(p => p.Id == playerId)
			.ExecuteUpdateAsync(s => s
				.SetProperty(p => p.AvatarExtension, extension)
				.SetProperty(p => p.AvatarUpdatedAt, at));
	}

	public async Task SetCover(int playerId, int? presetId, string? file, DateTime at)
	{
		await dbContext.Players
			.Where(p => p.Id == playerId)
			.ExecuteUpdateAsync(s => s
				.SetProperty(p => p.CoverPresetId, presetId)
				.SetProperty(p => p.CoverFile, file)
				.SetProperty(p => p.CoverUpdatedAt, at));
	}
	
	private static string? Normalize(string value)
	{
		var trimmed = value.Trim();

		return trimmed.Length == 0 ? null : trimmed;
	}
}