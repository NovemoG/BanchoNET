using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Lazer.Metadata;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.BeatmapServer.Services;

public class BeatmapsRepository(
	BanchoDbContext dbContext,
	IScoresRepository scores,
	IBeatmapService beatmapCache,
	ISearchProjectionSyncService projectionSync,
	IBeatmapChangeNotifier changeNotifier
) : IBeatmapsRepository
{
	private const int ChangesPerRequest = 500;

	public async Task<List<BeatmapsetDto>> GetRandomBeatmaps() {
		var count = await dbContext.Beatmapsets.CountAsync();
		
		var randomIndex = new Random().Next(0, Math.Max(0, count - 51));

		return await dbContext.Beatmapsets
			.AsNoTracking()
			.AsSplitQuery()
			.Include(bs => bs.Beatmaps)
				.ThenInclude(b => b.Collaborators)
			.Include(bs => bs.BeatmapsetFavorites)
				.ThenInclude(bf => bf.Player)
			.Include(bs => bs.Creator)
			.OrderBy(bs => bs.Id)
			.Skip(randomIndex)
			.Take(50)
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
			.FirstOrDefaultAsync(b => b.MD5 == checksum.ToUpper());
	}

	public async Task<BeatmapsetDto?> GetBeatmapset(
		int setId
	) {
		return await dbContext.Beatmapsets
			.AsNoTracking()
			.AsSplitQuery()
			.Include(bs => bs.Beatmaps)
				.ThenInclude(b => b.Collaborators)
			.FirstOrDefaultAsync(b => b.Id == setId);
	}

	public async Task<BeatmapsetDto?> GetBeatmapsetFull(
		int setId
	) {
		return await dbContext.Beatmapsets
			.AsNoTracking()
			.AsSplitQuery()
			.Include(bs => bs.Beatmaps)
				.ThenInclude(b => b.Collaborators)
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
				p.SetProperty(b => b.MaximumStatistics, statistics)
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
	/// Changes the status of one beatmap. Its set then takes the status of its most official beatmap.
	/// </summary>
	/// <param name="targetStatus">Target status to which the current one will be changed</param>
	/// <param name="mapId">Id of a beatmap to update</param>
	public async Task<int> UpdateBeatmapStatus(
		int mapId,
		BeatmapStatus targetStatus
	) {
		if (mapId < 1) return 0;
		
		var setIds = await dbContext.Beatmaps
			.Where(b => b.Id == mapId)
			.Select(b => b.SetId)
			.ToListAsync();

		var affected = await dbContext.Beatmaps.Where(b => b.Id == mapId)
			.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, targetStatus));
		
		await ApplyStatusChanges(setIds, [mapId]);

		return affected;
	}
	
	/// <summary>
	/// Changes the status of every beatmap in a set.
	/// </summary>
	public async Task<int> UpdateBeatmapsetStatus(
		int setId,
		BeatmapStatus targetStatus
	) {
		if (setId < 1) return 0;
		
		var mapIds = await dbContext.Beatmaps
			.Where(b => b.SetId == setId)
			.Select(b => b.Id)
			.ToListAsync();

		await dbContext.Beatmaps.Where(b => b.SetId == setId)
			.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, targetStatus));
		
		return await ApplyStatusChanges([setId], mapIds);
	}
	
	public async Task<int> RestoreOfficialStatuses(
		int setId,
		IReadOnlyDictionary<int, BeatmapStatus> officialStatuses,
		int mapId = -1
	) {
		if (setId < 1) return 0;
		
		var mapIds = await dbContext.Beatmaps
			.Where(b => b.SetId == setId && (mapId < 1 || b.Id == mapId))
			.Select(b => b.Id)
			.ToListAsync();

		foreach (var group in mapIds.GroupBy(id => officialStatuses.GetValueOrDefault(id, BeatmapStatus.LatestPending)))
		{
			var ids = group.ToList();

			await dbContext.Beatmaps.Where(b => ids.Contains(b.Id))
				.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, group.Key));
		}

		await ApplyStatusChanges([setId], mapIds);

		return mapIds.Count;
	}

	public async Task<List<int>> RankAllPending() {
		var pending = await dbContext.Beatmaps
			.Where(b => b.Status < BeatmapStatus.Ranked)
			.Select(b => new { b.Id, b.SetId })
			.ToListAsync();

		await dbContext.Beatmaps.Where(b => b.Status < BeatmapStatus.Ranked)
			.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, BeatmapStatus.Ranked));
		
		var setIds = pending.Select(b => b.SetId).Distinct().ToList();

		await ApplyStatusChanges(setIds, pending.Select(b => b.Id).ToList());

		return setIds;
	}

	public async Task<BeatmapUpdates> GetBeatmapsetChangesSince(
		int queueId
	) {
		var changes = await dbContext.BeatmapsetChanges
			.AsNoTracking()
			.Where(c => c.Id > queueId)
			.OrderBy(c => c.Id)
			.Take(ChangesPerRequest)
			.ToListAsync();

		if (changes.Count > 0)
			return new BeatmapUpdates(changes.Select(c => c.BeatmapsetId).Distinct().ToArray(), changes[^1].Id);

		// also moves clients past ids this server never handed out, so they don't ask for them forever
		var latest = await dbContext.BeatmapsetChanges.MaxAsync(c => (int?)c.Id) ?? 0;
		return new BeatmapUpdates([], latest);
	}
	
	private async Task<int> ApplyStatusChanges(
		IReadOnlyCollection<int> setIds,
		IReadOnlyCollection<int> mapIds
	) {
		if (setIds.Count == 0) return 0;

		var sets = await dbContext.Beatmapsets
			.AsNoTracking()
			.Where(bs => setIds.Contains(bs.Id))
			.Select(bs => new
			{
				bs.Id,
				bs.Status,
				bs.RankedDate,
				bs.LastUpdated,
				Beatmaps = bs.Beatmaps.Select(b => new { b.Id, b.Status }).ToList()
			})
			.ToListAsync();

		foreach (var set in sets)
		{
			var status = set.Beatmaps.Select(b => b.Status).ToBeatmapsetStatus();
			// ranking keeps the set's last update as its ranked date, so it doesn't move in date sorted searches
			var rankedDate = status.HasLeaderboard() ? set.RankedDate ?? set.LastUpdated : (DateTimeOffset?)null;

			if (status != set.Status || rankedDate != set.RankedDate)
			{
				await dbContext.Beatmapsets.Where(bs => bs.Id == set.Id)
					.ExecuteUpdateAsync(p => p
						.SetProperty(bs => bs.Status, status)
						.SetProperty(bs => bs.RankedDate, rankedDate));
			}

			if (beatmapCache.GetBeatmapset(set.Id) is not { } cached) continue;

			cached.Status = status;
			cached.RankedDate = rankedDate?.UtcDateTime;

			foreach (var beatmap in cached.Beatmaps)
			{
				if (set.Beatmaps.FirstOrDefault(b => b.Id == beatmap.Id) is { } updated)
					beatmap.Status = updated.Status;
			}
		}

		var foundSetIds = sets.Select(s => s.Id).ToArray();

		await projectionSync.RefreshAsync(dbContext, [], foundSetIds, CancellationToken.None);
		await scores.RefreshScoresRankedStatus(mapIds);
		await QueueBeatmapsetChanges(foundSetIds);

		return sets.Count;
	}

	private async Task QueueBeatmapsetChanges(
		int[] setIds
	) {
		if (setIds.Length == 0) return;

		var now = DateTimeOffset.UtcNow;
		var changes = setIds.Select(id => new BeatmapsetChange { BeatmapsetId = id, ChangedAt = now }).ToList();

		dbContext.BeatmapsetChanges.AddRange(changes);
		await dbContext.SaveChangesAsync();

		await changeNotifier.BeatmapsetsChanged(setIds, changes.Max(c => c.Id));
	}

	public async Task InsertBeatmapset(
		Beatmapset set
	) {
		var beatmapset = await dbContext.Beatmapsets
			.AsSplitQuery()
			.Include(bs => bs.Beatmaps)
				.ThenInclude(b => b.Collaborators)
			.FirstOrDefaultAsync(bs => bs.Id == set.Id);
		
		if (beatmapset == null)
		{
			dbContext.Beatmapsets.Add(set.ToDto());
			
			await dbContext.SaveChangesAsync();
			return;
		}
		
		var previousStatus = beatmapset.Status;
		var previousRankedDate = beatmapset.RankedDate;
		var previousMapStatuses = beatmapset.Beatmaps.ToDictionary(b => b.Id, b => b.Status);

		beatmapset.UpdateWith(set);
		
		var incomingBeatmapIds = set.Beatmaps
			.Select(b => b.Id)
			.ToHashSet();

		// Remove beatmaps that no longer exist in the new beatmapset
		foreach (var oldBeatmap in beatmapset.Beatmaps.ToList()
			         .Where(oldBeatmap => !incomingBeatmapIds.Contains(oldBeatmap.Id)))
		{
			//await scores.ToggleBeatmapScoresVisibility(oldBeatmap.MD5, false);
			dbContext.Beatmaps.Remove(oldBeatmap);
		}
		
		foreach (var beatmap in set.Beatmaps)
		{
			var beatmaps = beatmapset.Beatmaps;
			
			var dbBeatmap = beatmaps.FirstOrDefault(b => b.Id == beatmap.Id);
			if (dbBeatmap != null)
			{
				if (!dbBeatmap.MD5.Equals(beatmap.Checksum, StringComparison.OrdinalIgnoreCase))
				{
					await scores.ToggleBeatmapScoresVisibility(dbBeatmap.MD5.ToLower());
					dbBeatmap.MaximumStatistics = new Dictionary<HitResult, int>();
				}
				dbContext.Update(dbBeatmap.UpdateWith(beatmap));
			}
			else
			{
				dbContext.Beatmaps.Add(beatmap.ToDto());
			}
		}
		
		beatmapset.ApplyBeatmapStatuses(set);

		await dbContext.SaveChangesAsync();

		var changedMapIds = set.Beatmaps
			.Where(b => previousMapStatuses.TryGetValue(b.Id, out var status) && status != b.Status)
			.Select(b => b.Id)
			.ToList();

		await scores.RefreshScoresRankedStatus(changedMapIds);

		if (changedMapIds.Count > 0 || beatmapset.Status != previousStatus || beatmapset.RankedDate != previousRankedDate)
			await QueueBeatmapsetChanges([set.Id]);
	}
}