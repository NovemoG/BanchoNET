using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.BeatmapServer.Services;

public class BeatmapsRepository(
	BanchoDbContext dbContext,
	ILegacyScoresRepository scores
) : IBeatmapsRepository
{
	public async Task<List<BeatmapsetDto>> GetRandomBeatmaps() {
		var count = await dbContext.Beatmapsets.CountAsync();
		
		var randomIndex = new Random().Next(0, Math.Max(0, count - 11));

		return await dbContext.Beatmapsets
			.AsNoTracking()
			.AsSplitQuery()
			.Include(bs => bs.Beatmaps)
				.ThenInclude(b => b.Owners)
					.ThenInclude(bo => bo.Player)
			.Include(bs => bs.BeatmapsetFavorites)
				.ThenInclude(bf => bf.Player)
			.Include(bs => bs.Creator)
			.OrderBy(bs => bs.Id)
			.Skip(randomIndex)
			.Take(10)
			.ToListAsync();
	}
	
	public async Task<BeatmapDto?> GetBeatmap(
		int mapId
	) {
		return await dbContext.Beatmaps
			.AsNoTracking()
			.FirstOrDefaultAsync(b => b.Id == mapId);
	}

	public async Task<BeatmapDto?> GetBeatmap(
		string checksum
	) {
		return await dbContext.Beatmaps
			.AsNoTracking()
			.FirstOrDefaultAsync(b => b.MD5 == checksum);
	}

	public async Task<BeatmapsetDto?> GetBeatmapset(
		int setId
	) {
		return await dbContext.Beatmapsets
			.AsNoTracking()
			.Include(bs => bs.Beatmaps)
			.FirstOrDefaultAsync(b => b.Id == setId);
	}

	public async Task<BeatmapsetDto?> GetBeatmapsetFull(
		int setId
	) {
		return await dbContext.Beatmapsets
			.AsNoTracking()
			.AsSplitQuery()
			.Include(bs => bs.Beatmaps)
				.ThenInclude(b => b.Owners)
					.ThenInclude(bo => bo.Player)
			.Include(bs => bs.BeatmapsetFavorites)
				.ThenInclude(bf => bf.Player)
			.Include(bs => bs.Creator)
			.FirstOrDefaultAsync(bs => bs.Id == setId);
	}

	public async Task<List<BeatmapPlays>> FetchPlayerPlaycount(
		int setId,
		int playerId
	) {
		return await dbContext.BeatmapPlays
			.AsNoTracking()
			.Include(b => b.Beatmap)
			.Where(b => b.Beatmap.SetId == setId && b.PlayerId == playerId)
			.ToListAsync();
	}

	public async Task<bool> FetchPlayerFavorited(
		int setId,
		int playerId
	) {
		return await dbContext.BeatmapsetFavorites
			.AsNoTracking()
			.Where(bf => bf.BeatmapsetId == setId && bf.PlayerId == playerId)
			.AnyAsync();
	}

	public async Task<List<int>> GetPlayerFavoriteBeatmapsets(
		int playerId,
		int offset = 0,
		int count = 7
	) {
		return await dbContext.BeatmapsetFavorites
			.AsNoTracking()
			.Where(bf => bf.PlayerId == playerId)
			.OrderByDescending(bf => bf.FavoriteAt)
			.Skip(offset)
			.Take(count)
			.Select(bf => bf.BeatmapsetId)
			.ToListAsync();
	}

	public async Task<List<BeatmapPlays>> GetPlayerMostPlayedBeatmaps(
		int playerId,
		int offset = 0,
		int count = 6
	) {
		return await dbContext.BeatmapPlays
			.AsNoTracking()
			.Include(bp => bp.Beatmap)
			.ThenInclude(b => b.Beatmapset)
			.Where(bp => bp.PlayerId == playerId)
			.OrderByDescending(bp => bp.Plays)
			.Skip(offset)
			.Take(count)
			.ToListAsync();
	}

	public async Task UpdateBeatmapPlayCount(
		Beatmap beatmap,
		int playerId
	) {
		await dbContext.Beatmaps
			.Where(b => b.Id == beatmap.Id)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(b => b.Plays, beatmap.Plays)
					.SetProperty(b => b.Passes, beatmap.Passes)
			);
		await dbContext.Beatmapsets
			.Where(bs => bs.Id == beatmap.BeatmapsetId)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(bs => bs.PlayCount, beatmap.Set.PlayCount)
			);
		
		var play = await dbContext.BeatmapPlays
			.SingleOrDefaultAsync(bp => bp.BeatmapId == beatmap.Id && bp.PlayerId == playerId);

		if (play is null)
		{
			dbContext.BeatmapPlays.Add(new BeatmapPlays
			{
				BeatmapId = beatmap.Id,
				PlayerId = playerId,
				Plays = 1
			});
		}
		else play.Plays += 1;

		await dbContext.SaveChangesAsync();
	}

	public async Task UpdateBeatmapsetFavoriteCount(
		Beatmapset beatmapset,
		int playerId,
		bool add
	) {
		var play = await dbContext.BeatmapsetFavorites
			.SingleOrDefaultAsync(bf => bf.BeatmapsetId == beatmapset.Id && bf.PlayerId == playerId);

		if (play is null && add)
		{
			dbContext.BeatmapsetFavorites.Add(new BeatmapsetFavorite
			{
				BeatmapsetId = beatmapset.Id,
				PlayerId = playerId,
			});
		}
		else if (play is not null && !add)
		{
			dbContext.BeatmapsetFavorites.Remove(play);
		}
		
		await dbContext.SaveChangesAsync();
		await dbContext.Beatmapsets.Where(s => s.Id == beatmapset.Id)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(s => s.FavoriteCount, beatmapset.FavoriteCount)
			);
	}

	public async Task UpdateBeatmapMaxStatistics(
		Beatmap beatmap,
		Dictionary<HitResult, int> statistics
	) {
		if (beatmap.MaxStatistics.Any(s => s.Value > 0)) return;
		
		beatmap.MaxStatistics = statistics;
		
		await dbContext.Beatmaps
			.Where(b => b.Id == beatmap.Id)
			.ExecuteUpdateAsync(p =>
				p.SetProperty(b => b.MaximumStatistics, beatmap.MaxStatistics)
			);
	}

	public async Task UpdateBeatmapFailTimes(
		int beatmapId,
		int index,
		bool isFail
	) {
		var column = isFail ? "Fails" : "Exits";

#pragma warning disable EF1002
		await dbContext.Database.ExecuteSqlRawAsync(
			$"""

			         UPDATE "Beatmaps"
			         SET "{column}"[{index + 1}] = "{column}"[{index + 1}] + 1
			         WHERE "Id" = {beatmapId};
			     
			 """
		);
#pragma warning restore EF1002
	}

	/// <summary>
	/// Changes beatmap status in the database
	/// </summary>
	/// <param name="targetStatus">Target status to which the current one will be changed</param>
	/// <param name="mapId">Id of a beatmap to update</param>
	/// <returns>Number of maps that were affected</returns>
	public async Task<int> UpdateBeatmapStatus(
		int mapId,
		BeatmapStatus targetStatus
	) {
		if (mapId < 1) return 0;
		
		return await dbContext.Beatmaps.Where(b => b.Id == mapId)
				.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, targetStatus));
	}
	
	public async Task<int> UpdateBeatmapsetStatus(
		int setId,
		BeatmapStatus targetStatus
	) {
		if (setId < 1) return 0;
		
		return await dbContext.Beatmaps.Where(b => b.SetId == setId)
			.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, targetStatus));
	}

	public async Task InsertBeatmapset(
		Beatmapset set
	) {
		var beatmapset = await dbContext.Beatmapsets
			.Include(bs => bs.Beatmaps)
			.FirstOrDefaultAsync(bs => bs.Id == set.Id);
		
		if (beatmapset == null)
		{
			dbContext.Beatmapsets.Add(set.ToDto());
			
			await dbContext.SaveChangesAsync();
			return;
		}
		
		beatmapset.UpdateWith(set);
		
		var incomingBeatmapIds = set.Beatmaps
			.Select(b => b.Id)
			.ToHashSet();

		// Remove beatmaps that no longer exist in the new beatmapset
		foreach (var oldBeatmap in beatmapset.Beatmaps.ToList()
			         .Where(oldBeatmap => !incomingBeatmapIds.Contains(oldBeatmap.Id)))
		{
			await scores.ToggleBeatmapScoresVisibility(oldBeatmap.MD5, false);
			dbContext.Beatmaps.Remove(oldBeatmap);
		}
		
		foreach (var beatmap in set.Beatmaps)
		{
			var beatmaps = beatmapset.Beatmaps;
			
			var dbBeatmap = beatmaps.FirstOrDefault(b => b.Id == beatmap.Id);
			if (dbBeatmap != null)
			{
				await scores.ToggleBeatmapScoresVisibility(dbBeatmap.MD5, false);
				dbContext.Update(dbBeatmap.UpdateWith(beatmap, beatmapset.IsRankedOfficially));
			}
			else
			{
				dbContext.Beatmaps.Add(beatmap.ToDto());
			}
		}
		
		await dbContext.SaveChangesAsync();
	}
}