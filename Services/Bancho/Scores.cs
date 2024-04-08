using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private static bool OrderByPp(GameMode mode) => mode >= GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
	
	public async Task InsertScore(Score score, string beatmapMD5, string username)
	{
		var dbScore = await _dbContext.Scores.AddAsync(new ScoreDto
		{
			BeatmapMD5 = beatmapMD5,
			PP = score.PP,
			Acc = score.Acc,
			Score = score.TotalScore,
			MaxCombo = score.MaxCombo,
			Mods = (int)score.Mods,
			Count300 = score.Count300,
			Count100 = score.Count100,
			Count50 = score.Count50,
			Misses = score.Misses,
			Gekis = score.Gekis,
			Katus = score.Katus,
			Grade = (byte)score.Grade,
			Status = (byte)score.Status,
			Mode = (byte)score.Mode,
			PlayTime = score.ClientTime,
			TimeElapsed = score.TimeElapsed,
			ClientFlags = (int)score.ClientFlags,
			PlayerId = score.PlayerId,
			Username = username,
			Perfect = score.Perfect,
			OnlineChecksum = score.ClientChecksum
		});
		await _dbContext.SaveChangesAsync();

		score.Id = dbScore.Entity.Id;
	}

	public async Task<Score?> GetScore(string checksum)
	{
		if (!string.IsNullOrEmpty(checksum))
		{
			var score = await _dbContext.Scores.FirstOrDefaultAsync(s => s.OnlineChecksum == checksum);
			return score == null ? null : new Score(score);
		}

		return null;
	}

	public async Task<Score?> GetPlayerBestScoreOnMap(
		Player player,
		string beatmapMD5,
		GameMode mode)
	{
		var score = await _dbContext.Scores.FirstOrDefaultAsync(
			s => s.PlayerId == player.Id &&
			     s.BeatmapMD5 == beatmapMD5 &&
			     s.Mode == (int)mode &&
			     s.Status == (int)SubmissionStatus.Best);

		return score == null ? null : new Score(score);
	}
	
	public async Task<Score?> GetPlayerBestScoreOnLeaderboard(
		Player player,
		Beatmap beatmap,
		GameMode mode,
		bool withMods,
		Mods mods = Mods.None,
		bool calculatePlacement = true)
	{
		Score? score;
		if (withMods)
		{
			var result = await _dbContext.Scores.Where(s => s.BeatmapMD5 == beatmap.MD5 &&
			                                   s.Mode == (int)mode &&
			                                   s.Status > 0 &&
			                                   s.PlayerId == player.Id &&
			                                   s.Mods == (int)mods)
			                .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.Score)
			                .FirstOrDefaultAsync();

			if (result == null) return null;
			
			score = new Score(result);
		}
		else
		{
			score = await GetPlayerBestScoreOnMap(player, beatmap.MD5, mode);

			if (score == null) return null;
		}

		if (calculatePlacement)
			await SetScoreLeaderboardPosition(beatmap, score, withMods, mods);
		
		return score;
	}

	public async Task UpdatePlayerBestScoreOnMap(Beatmap beatmap, Score score)
	{
		await _dbContext.Scores.Where(s => s.PlayerId == score.PlayerId && 
		                                   s.BeatmapMD5 == beatmap.MD5 &&
		                                   s.Mode == (int)beatmap.Mode &&
		                                   s.Status == (int)SubmissionStatus.Best)
		                       .ExecuteUpdateAsync(s => 
			                       s.SetProperty(u => u.Status, (int)SubmissionStatus.Submitted));
	}

	public async Task<ScoreDto?> GetBestBeatmapScore(Beatmap beatmap, GameMode mode)
	{
		return await _dbContext.Scores.Include(s => s.Player)
		                       .Where(s => s.BeatmapMD5 == beatmap.MD5 &&
		                                   s.Mode == (int)mode &&
		                                   s.Status == (int)SubmissionStatus.Best &&
		                                   (s.Player.Privileges & 1) == 1)
		                       .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.Score)
		                       .FirstOrDefaultAsync();
	}
	
	public async Task SetScoreLeaderboardPosition(
		Beatmap beatmap,
		Score score,
		bool withMods,
		Mods mods = Mods.None)
	{
		score.LeaderboardPosition = await (withMods
				? _dbContext.Scores.Include(s => s.Player)
				            .Where(s => s.BeatmapMD5 == beatmap.MD5 &&
				                        s.Mode == (int)score.Mode &&
				                        s.Status > 0 &&
				                        (s.Player.Privileges & 1) == 1 &&
				                        s.Mods == (int)mods &&
				                        (OrderByPp(score.Mode)
					                        ? score.PP < s.PP
					                        : score.TotalScore < s.Score))
				            .GroupBy(s => s.PlayerId)
				            .Select(s => s.First())
				: _dbContext.Scores.Include(s => s.Player)
				            .Where(s => s.BeatmapMD5 == beatmap.MD5 &&
				                        s.Mode == (int)score.Mode &&
				                        s.Status == (int)SubmissionStatus.Best &&
				                        (s.Player.Privileges & 1) == 1 &&
				                        (OrderByPp(score.Mode)
					                        ? score.PP < s.PP
					                        : score.TotalScore < s.Score)))
			.CountAsync() + 1;
	}
	
	public async Task<List<ScoreDto>> GetBeatmapLeaderboard(
		string beatmapMD5,
		GameMode mode,
		LeaderboardType type,
		Mods mods,
		Player player)
	{
		var isCountry = type == LeaderboardType.Country;
		var countryCode = player.Geoloc.Country.Acronym;
		
		var withMods = type is LeaderboardType.Mods or LeaderboardType.CountryMods or LeaderboardType.FriendsMods;
		
		var withFriendsList = type == LeaderboardType.Friends;
		var friendIds = withFriendsList ? player.Friends.ToHashSet() : [];

		var result = await _dbContext.Scores.AsNoTracking().Include(s => s.Player)
		                             .Where(s => s.BeatmapMD5 == beatmapMD5 &&
		                                         s.Mode == (int)mode &&
		                                         (withMods
			                                         ? s.Status > 0
			                                         : s.Status == (int)SubmissionStatus.Best) &&
		                                         (s.Player.Privileges & 1) == 1 &&
		                                         (!withMods || s.Mods == (int)mods) &&
		                                         (!isCountry || s.Player.Country == countryCode) &&
		                                         (!withFriendsList || friendIds.Contains(s.PlayerId)))
		                             .GroupBy(s => s.PlayerId)
		                             .Select(g => g.OrderByDescending(s => OrderByPp(mode) ? s.PP : s.Score).First())
		                             .Take(AppSettings.ScoresOnLeaderboard)
		                             .ToListAsync();

		//TODO optimize this query somehow or change database structure so it does not suck 
		
		return result;
	}
}