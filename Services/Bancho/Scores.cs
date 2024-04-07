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
	
	public async Task InsertScore(Score score)
	{
		var dbScore = await _dbContext.Scores.AddAsync(new ScoreDto
		{
			BeatmapMD5 = score.BeatmapMD5,
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
			PlayTime = score.ServerTime,
			TimeElapsed = score.TimeElapsed,
			ClientFlags = (int)score.ClientFlags,
			PlayerId = score.PlayerId,
			Username = score.Player.Username,
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
			     s.Mode == (byte)mode &&
			     s.Status == (byte)SubmissionStatus.Best);

		return score == null ? null : new Score(score);
	}
	
	public async Task<Score?> GetPlayerBestScoreOnLeaderboard(
		Player player,
		Beatmap beatmap,
		GameMode mode,
		bool withMods,
		Mods mods = Mods.None)
	{
		Score? score;
		if (withMods)
		{
			var result = await _dbContext.Scores.Where(s => s.BeatmapMD5 == beatmap.MD5 &&
			                                   s.Mode == (byte)mode &&
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

		await SetScoreLeaderboardPosition(beatmap, score, withMods, mods);
		
		return score;
	}

	public async Task UpdatePlayerBestScoreOnMap(Beatmap beatmap, Score score)
	{
		await _dbContext.Scores.Where(s => s.PlayerId == score.PlayerId && 
		                                   s.BeatmapMD5 == beatmap.MD5 &&
		                                   s.Mode == (byte)beatmap.Mode &&
		                                   s.Status == (byte)SubmissionStatus.Best)
		                       .ExecuteUpdateAsync(s => 
			                       s.SetProperty(u => u.Status, (byte)SubmissionStatus.Submitted));
	}

	public async Task<ScoreDto?> GetBestBeatmapScore(Beatmap beatmap, GameMode mode)
	{
		return await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .Where(j => j.s.BeatmapMD5 == beatmap.MD5 &&
		                                   j.s.Mode == (byte)mode &&
		                                   j.s.Status == (byte)SubmissionStatus.Best &&
		                                   (j.u.Privileges & 1) == 1)
		                       .OrderByDescending(j => OrderByPp(mode) ? j.s.PP : j.s.Score)
		                       .Select(j => j.s)
		                       .FirstOrDefaultAsync();
	}

	//TODO does not work correctly
	public async Task SetScoreLeaderboardPosition(
		Beatmap beatmap,
		Score score,
		bool withMods,
		Mods mods = Mods.None)
	{
		score.LeaderboardPosition =
			await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
			                .CountAsync(j => j.s.BeatmapMD5 == beatmap.MD5 &&
			                                 beatmap.Mode == score.Mode &&
			                                 (OrderByPp(score.Mode)
				                                 ? score.PP < j.s.PP
				                                 : score.TotalScore < j.s.Score) &&
			                                 score.Status == SubmissionStatus.Best &&
			                                 (j.u.Privileges & 1) == 1 &&
			                                 (!withMods || j.s.Mods == (int)mods)) + 1;
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
		
		return await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .Where(j => j.s.BeatmapMD5 == beatmapMD5 &&
		                                   j.s.Mode == (byte)mode &&
		                                   (withMods ?
			                                   j.s.Status > 0 :
			                                   j.s.Status == (byte)SubmissionStatus.Best) &&
		                                   (j.u.Privileges & 1) == 1 &&
		                                   (!withMods || j.s.Mods == (int)mods) &&
		                                   (!isCountry || j.u.Country == countryCode) &&
		                                   (!withFriendsList || friendIds.Contains(j.s.PlayerId)))
		                       .OrderByDescending(j => OrderByPp(mode) ? j.s.PP : j.s.Score)
		                       .Take(AppSettings.ScoresOnLeaderboard)
		                       .Select(j => j.s)
		                       .ToListAsync();
	}
	
	private async Task<List<ScoreDto>> GetBeatmapTopLeaderboard(string beatmapMD5, GameMode mode)
	{
		return await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .Where(j => j.s.BeatmapMD5 == beatmapMD5 &&
		                                   j.s.Mode == (byte)mode &&
		                                   j.s.Status == (byte)SubmissionStatus.Best &&
		                                   (j.u.Privileges & 1) == 1)
		                       .OrderByDescending(j => OrderByPp(mode) ? j.s.PP : j.s.Score)
		                       .Take(AppSettings.ScoresOnLeaderboard)
		                       .Select(j => j.s)
		                       .ToListAsync();
	}
	
	private async Task<List<ScoreDto>> GetBeatmapCountryLeaderboard(string beatmapMD5, GameMode mode, Player player)
	{
		var countryCode = player.Geoloc.Country.Acronym;
		
		return await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .Where(j => j.s.BeatmapMD5 == beatmapMD5 &&
		                                   j.s.Mode == (byte)mode &&
		                                   j.s.Status == (byte)SubmissionStatus.Best &&
		                                   (j.u.Privileges & 1) == 1 &&
		                                   j.u.Country == countryCode)
		                       .OrderByDescending(j => OrderByPp(mode) ? j.s.PP : j.s.Score)
		                       .Take(AppSettings.ScoresOnLeaderboard)
		                       .Select(j => j.s)
		                       .ToListAsync();
	}

	private async Task<List<ScoreDto>> GetBeatmapModsLeaderboard(string beatmapMD5, GameMode mode, Mods mods)
	{
		return await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .Where(j => j.s.BeatmapMD5 == beatmapMD5 &&
		                                   j.s.Mode == (byte)mode &&
		                                   j.s.Status != (byte)SubmissionStatus.Failed &&
		                                   (j.u.Privileges & 1) == 1 &&
		                                   j.s.Mods == (int)mods)
		                       .OrderByDescending(j => OrderByPp(mode) ? j.s.PP : j.s.Score)
		                       .Take(AppSettings.ScoresOnLeaderboard)
		                       .Select(j => j.s)
		                       .ToListAsync();
	}
	
	private async Task<List<ScoreDto>> GetBeatmapFriendsLeaderboard(string beatmapMD5, GameMode mode, Player player)
	{
		//TODO add support for mods in this query
		
		var friendIds = player.Friends.ToHashSet();
		
		return await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .Where(j => j.s.BeatmapMD5 == beatmapMD5 &&
		                                   j.s.Mode == (byte)mode &&
		                                   j.s.Status == (byte)SubmissionStatus.Best &&
		                                   (j.u.Privileges & 1) == 1 &&
		                                   friendIds.Contains(j.s.PlayerId))
		                       .OrderByDescending(j => OrderByPp(mode) ? j.s.PP : j.s.Score)
		                       .Take(AppSettings.ScoresOnLeaderboard)
		                       .Select(j => j.s)
		                       .ToListAsync();
	}
}