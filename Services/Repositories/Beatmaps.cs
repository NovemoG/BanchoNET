using BanchoNET.Abstractions.Repositories;
using BanchoNET.Abstractions.Services;
using BanchoNET.Models;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Utils.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class BeatmapsRepository(
	IBanchoSession session,
	BanchoDbContext dbContext,
	IBeatmapHandler beatmapHandler,
	ScoresRepository scores
	) : IBeatmapsRepository
{
	public async Task<Beatmap?> GetBeatmap(int mapId, int setId = -1)
	{
		var beatmap = session.GetBeatmapById(mapId);
		if (beatmap != null) return beatmap;

		if (setId < 1)
		{
			var map = await dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MapId == mapId);
			
			if (map != null)
				setId = map.SetId;
			else
			{
				var apiMap = await beatmapHandler.GetBeatmapFromApi(mapId: mapId);
				if (apiMap == null) return null;

				setId = apiMap.SetId;
			}
		}

		var beatmapSet = await GetBeatmapSet(setId, mapId);

		return beatmapSet != null
			? beatmapSet.Beatmaps.FirstOrDefault(b => b.MapId == mapId)
			: beatmap;
	}
	
	public async Task<Beatmap?> GetBeatmap(string beatmapMD5, int setId = -1)
	{
		var beatmap = session.GetBeatmapByMD5(beatmapMD5);
		if (beatmap != null) return beatmap;
		
		var mapId = 0;
		if (setId < 1)
		{
			var map = await dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MD5 == beatmapMD5);

			if (map != null)
			{
				setId = map.SetId;
				mapId = map.MapId;
			}
			else
			{
				var apiMap = await beatmapHandler.GetBeatmapFromApi(beatmapMD5: beatmapMD5);
				if (apiMap == null) return null;

				setId = apiMap.SetId;
				mapId = apiMap.MapId;
			}
		}

		var beatmapSet = await GetBeatmapSet(setId, mapId);
		
		return beatmapSet != null
			? beatmapSet.Beatmaps.FirstOrDefault(b => b.MD5 == beatmapMD5)
			: beatmap;
	}
	
	public async Task<BeatmapSet?> GetBeatmapSet(int setId, int mapId = -1)
	{
		var didApiRequest = false;
		var beatmapSet = session.GetBeatmapSet(setId);
		
		if (beatmapSet == null)
		{
			var dbBeatmaps = await dbContext.Beatmaps
				.Where(b => b.SetId == setId)
				.ToListAsync();

			if (dbBeatmaps.Count == 0)
			{
				beatmapSet = await beatmapHandler.GetBeatmapSetFromApi(setId);
				if (beatmapSet == null) return null;

				didApiRequest = true;
				await InsertBeatmapSet(beatmapSet);
			}
			else beatmapSet = new BeatmapSet(dbBeatmaps);
			
			session.CacheBeatmapSet(beatmapSet);
		}

		if (!didApiRequest && mapId > 0)
			if (beatmapSet.Beatmaps.All(b => b.MapId != mapId) /*or expired (maps can be updated without md5 being changed)*/)
				await UpdateBeatmapSet(setId);
		
		return beatmapSet;
	}
	
	public async Task UpdateBeatmapSet(int setId)
	{
		var beatmapSet = await beatmapHandler.GetBeatmapSetFromApi(setId);
		if (beatmapSet == null) return;
		
		session.CacheBeatmapSet(beatmapSet);
		await InsertBeatmapSet(beatmapSet);
	}
	
	/// <summary>
	/// Updates Playcount and Passcount of a beatmap in database
	/// </summary>
	/// <param name="beatmap">Beatmap for which to update stats</param>
	public async Task UpdateBeatmapPlayCount(Beatmap beatmap)
	{
		await dbContext.Beatmaps.Where(b => b.MapId == beatmap.MapId)
		                .ExecuteUpdateAsync(p => 
			                p.SetProperty(b => b.Plays, beatmap.Plays)
			                 .SetProperty(b => b.Passes, beatmap.Passes));
	}

	/// <summary>
	/// Changes beatmap status in cache and database
	/// </summary>
	/// <param name="targetStatus">Target status to which the current one will be changed</param>
	/// <param name="mapId">Id of a beatmap to update</param>
	/// <param name="setId">Id of a set to update</param>
	/// <returns>Number of maps that were affected</returns>
	public async Task<int> UpdateBeatmapStatus(
		BeatmapStatus targetStatus,
		int mapId)
	{
		if (mapId < 1) return 0;
		
		var cachedMap = await GetBeatmap(mapId);
		if (cachedMap == null) return 0;
		
		cachedMap.Status = targetStatus;
		
		return await dbContext.Beatmaps.Where(b => b.MapId == mapId)
				.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, (int)targetStatus));
	}
	
	public async Task<int> UpdateBeatmapSetStatus(
		BeatmapStatus targetStatus,
		int setId)
	{
		if (setId < 1) return 0;
		
		var cachedSet = await GetBeatmapSet(setId);
		if (cachedSet == null) return 0;
		
		foreach (var map in cachedSet.Beatmaps)
			map.Status = targetStatus;
				
		return await dbContext.Beatmaps.Where(b => b.SetId == setId)
			.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, (int)targetStatus));
	}

	public async Task InsertBeatmapSet(BeatmapSet set)
	{
		foreach (var beatmap in set.Beatmaps)
		{
			var dbBeatmap = await dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MapId == beatmap.MapId);

			if (dbBeatmap != null)
			{
				dbContext.Update(dbBeatmap.UpdateWith(beatmap));

				if (beatmap.Status <= BeatmapStatus.NotSubmitted)
					await scores.DisableNotSubmittedBeatmapScores(beatmap.MD5);
			}
			else
			{
				await dbContext.Beatmaps.AddAsync(beatmap.ToDto());
			}
		}
		
		await dbContext.SaveChangesAsync();
	}
}