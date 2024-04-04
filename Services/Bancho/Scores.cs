using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Players;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
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

	public async Task<Score?> GetPlayerBestScore(Player player, string beatmapMD5, GameMode mode)
	{
		var score = await _dbContext.Scores
            .Where(s => s.PlayerId == player.Id && 
                        s.BeatmapMD5 == beatmapMD5 &&
                        s.Mode == (byte)mode && 
                        s.Status == (byte)SubmissionStatus.Best)
            .FirstOrDefaultAsync();

		return score == null ? null : new Score(score);
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

	public async Task<(ScoreDto? Score, string Username)> GetBestBeatmapScore(Beatmap beatmap, GameMode mode, bool leaderboardByPP)
	{
		return mode >= GameMode.RelaxStd
			? await GetBestBeatmapScorePp(beatmap, mode)
			: leaderboardByPP
				? await GetBestBeatmapScorePp(beatmap, mode)
				: await GetBestBeatmapScoreScore(beatmap, mode);
	}
	
	private async Task<(ScoreDto?, string)> GetBestBeatmapScorePp(Beatmap beatmap, GameMode mode)
	{
		var result = await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                             .Where(j => j.s.BeatmapMD5 == beatmap.MD5 &&
		                                         j.s.Mode == (byte)mode &&
		                                         j.s.Status == (byte)SubmissionStatus.Best &&
		                                         (j.u.Privileges & 1) == 1)
		                             .OrderByDescending(j => j.s.Score)
		                             .Take(1)
		                             .FirstOrDefaultAsync();

		return result == null ? (null, "") : (result.s, result.u.Username);
	}
	
	private async Task<(ScoreDto?, string)> GetBestBeatmapScoreScore(Beatmap beatmap, GameMode mode)
	{
		var result = await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .Where(j => j.s.BeatmapMD5 == beatmap.MD5 &&
		                                   j.s.Mode == (byte)mode &&
		                                   j.s.Status == (byte)SubmissionStatus.Best &&
		                                   (j.u.Privileges & 1) == 1)
		                       .OrderByDescending(j => j.s.Score)
		                       .Take(1)
		                       .FirstOrDefaultAsync();
		
		return result == null ? (null, "") : (result.s, result.u.Username);
	}

	public async Task SetScoreLeaderboardPosition(Beatmap beatmap, Score score, bool leaderboardByPP)
	{
		score.LeaderboardPosition = score.Mode >= GameMode.RelaxStd
			? await LeaderboardPositionPp(beatmap, score)
			: leaderboardByPP
				? await LeaderboardPositionPp(beatmap, score)
				: await LeaderboardPositionScore(beatmap, score);
	}
	
	private async Task<int> LeaderboardPositionPp(Beatmap beatmap, Score score)
	{
		return await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .CountAsync(j => j.s.BeatmapMD5 == beatmap.MD5 &&
		                                        beatmap.Mode == score.Mode &&
		                                        score.PP > j.s.PP &&
		                                        score.Status == SubmissionStatus.Best &&
		                                        (j.u.Privileges & 1) == 1) + 1;
	}

	private async Task<int> LeaderboardPositionScore(Beatmap beatmap, Score score)
	{
		return await _dbContext.Scores.Join(_dbContext.Players, u => u.PlayerId, s => s.Id, (s, u) => new { u, s })
		                       .CountAsync(j => j.s.BeatmapMD5 == beatmap.MD5 &&
		                                        beatmap.Mode == score.Mode &&
		                                        score.TotalScore > j.s.Score &&
		                                        score.Status == SubmissionStatus.Best &&
		                                        (j.u.Privileges & 1) == 1) + 1;
	}
}