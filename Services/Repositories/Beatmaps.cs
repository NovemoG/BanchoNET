using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class BeatmapsRepository(BanchoDbContext dbContext, BeatmapHandler beatmapHandler)
{
	private readonly BanchoSession _session = BanchoSession.Instance;

	/// <summary>
	/// Updates Playcount and Passcount of a beatmap in database
	/// </summary>
	/// <param name="beatmap">Beatmap for which to update stats</param>
	public async Task UpdateBeatmapStats(Beatmap beatmap)
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
	/// <param name="beatmapId">Id of a beatmap to update</param>
	/// <param name="setId">Id of a set to update</param>
	/// <returns>Number of maps that were affected</returns>
	public async Task<int> ChangeBeatmapStatus(
		BeatmapStatus targetStatus,
		int beatmapId = -1,
		int setId = -1)
	{
		if (setId > -1)
		{
			var cachedSet = await GetBeatmapSet(setId);
			if (cachedSet != null)
			{
				foreach (var map in cachedSet.Beatmaps)
					map.Status = targetStatus;
				
				return await dbContext.Beatmaps.Where(b => b.SetId == setId)
					.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, (int)targetStatus));
			}
		}

		if (beatmapId <= -1) return 0;
		
		var cachedMap = await GetBeatmap(mapId: beatmapId);
		if (cachedMap == null) return 0;
		
		cachedMap.Status = targetStatus;
		cachedMap = _session.GetBeatmap(beatmapMD5: cachedMap.MD5);
		cachedMap!.Status = targetStatus; //Map exists because we got it by id
			
		return await dbContext.Beatmaps.Where(b => b.MapId == beatmapId)
				.ExecuteUpdateAsync(p => p.SetProperty(b => b.Status, (int)targetStatus));
	}

	public async Task<string> GetBeatmapFullName(int mapId = -1, string beatmapMD5 = "")
	{
		var map = await GetBeatmap(mapId: mapId, beatmapMD5: beatmapMD5);
		
		return map == null ? "" : map.FullName();
	}
	
	public async Task<Beatmap?> GetBeatmap(int mapId = -1, int setId = -1, string beatmapMD5 = "")
	{
		if (!string.IsNullOrEmpty(beatmapMD5))
			return await GetBeatmapWithMD5(beatmapMD5, setId);

		if (mapId > 0)
			return await GetBeatmapWithId(mapId);

		return null;
	}

	public async Task<Beatmap?> GetBeatmapWithMD5(string beatmapMD5, int setId)
	{
		var beatmap = _session.GetBeatmap(beatmapMD5: beatmapMD5);
		if (beatmap != null) return beatmap;
		
		var mapId = 0;
			
		if (setId <= 0)
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

	public async Task<Beatmap?> GetBeatmapWithId(int mapId)
	{
		var beatmap = _session.GetBeatmap(mapId: mapId);
		if (beatmap != null) return beatmap;
		
		int setId;
		var map = await dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MapId == mapId);
			
		if (map != null)
			setId = map.SetId;
		else
		{
			var apiMap = await beatmapHandler.GetBeatmapFromApi(mapId: mapId);
			if (apiMap == null) return null;

			setId = apiMap.SetId;
		}

		var beatmapSet = await GetBeatmapSet(setId, mapId);

		return beatmapSet != null
			? beatmapSet.Beatmaps.FirstOrDefault(b => b.MapId == mapId)
			: beatmap;
	}

	public async Task<BeatmapSet?> GetBeatmapSet(int setId, int mapId = 0)
	{
		var didApiRequest = false;
		var beatmapSet = _session.GetBeatmapSet(setId);
		
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
				_session.CacheBeatmapSet(beatmapSet);
				await InsertSetIntoDatabase(beatmapSet);
			}
			else
				beatmapSet = new BeatmapSet(dbBeatmaps);
		}

		if (!didApiRequest && mapId > 0)
			if (beatmapSet.Beatmaps.All(b => b.MapId != mapId))
				await UpdateBeatmapSet(setId);
		
		return beatmapSet;
	}

	public async Task UpdateBeatmapSet(int setId)
	{
		var beatmapSet = await beatmapHandler.GetBeatmapSetFromApi(setId);
		if (beatmapSet == null) return;
		
		_session.CacheBeatmapSet(beatmapSet);
		await InsertSetIntoDatabase(beatmapSet);
	}

	public async Task InsertSetIntoDatabase(BeatmapSet set)
	{
		foreach (var beatmap in set.Beatmaps)
		{
			var dbBeatmap = await dbContext.Beatmaps.FirstOrDefaultAsync(b => b.MapId == beatmap.MapId);

			if (dbBeatmap != null)
			{
				dbBeatmap = UpdateBeatmapDto(beatmap, dbBeatmap);
				dbContext.Update(dbBeatmap);
			}
			else
			{
				await dbContext.Beatmaps.AddAsync(CreateBeatmapDto(beatmap));
			}
		}
		
		await dbContext.SaveChangesAsync();
	}

	private static BeatmapDto CreateBeatmapDto(Beatmap beatmap)
	{
		return new BeatmapDto
		{
			MapId = beatmap.MapId,
			SetId = beatmap.SetId,
			Private = beatmap.Private,
			Mode = (byte)beatmap.Mode,
			Status = (sbyte)beatmap.Status,
			IsRankedOfficially = beatmap.IsRankedOfficially,
			MD5 = beatmap.MD5,
			Artist = beatmap.Artist,
			Title = beatmap.Title,
			Name = beatmap.Name,
			Creator = beatmap.Creator,
			SubmitDate = beatmap.SubmitDate,
			LastUpdate = beatmap.LastUpdate,
			TotalLength = beatmap.TotalLength,
			MaxCombo = beatmap.MaxCombo,
			Plays = beatmap.Plays,
			Passes = beatmap.Passes,
			Bpm = beatmap.Bpm,
			Cs = beatmap.Cs,
			Ar = beatmap.Ar,
			Od = beatmap.Od,
			Hp = beatmap.Hp,
			StarRating = beatmap.StarRating,
			NotesCount = beatmap.NotesCount,
			SlidersCount = beatmap.SlidersCount,
			SpinnersCount = beatmap.SpinnersCount
		};
	}

	private static BeatmapDto UpdateBeatmapDto(Beatmap beatmap, BeatmapDto currentBeatmap)
	{
		return new BeatmapDto
		{
			MapId = currentBeatmap.MapId,
			SetId = currentBeatmap.SetId,
			Private = currentBeatmap.Private,
			Mode = currentBeatmap.Mode,
			Status = beatmap.IsRankedOfficially || beatmap.Status == BeatmapStatus.Qualified
				? (sbyte)beatmap.Status
				: currentBeatmap.Status,
			IsRankedOfficially = beatmap.IsRankedOfficially,
			MD5 = beatmap.MD5,
			Artist = beatmap.Artist,
			Title = beatmap.Title,
			Name = beatmap.Name,
			Creator = beatmap.Creator,
			SubmitDate = beatmap.SubmitDate,
			LastUpdate = beatmap.LastUpdate,
			TotalLength = beatmap.TotalLength,
			MaxCombo = beatmap.MaxCombo,
			Plays = currentBeatmap.Plays,
			Passes = currentBeatmap.Passes,
			Bpm = beatmap.Bpm,
			Cs = beatmap.Cs,
			Ar = beatmap.Ar,
			Od = beatmap.Od,
			Hp = beatmap.Hp,
			StarRating = beatmap.StarRating,
			NotesCount = beatmap.NotesCount,
			SlidersCount = beatmap.SlidersCount,
			SpinnersCount = beatmap.SpinnersCount
		};
	}
}