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
		await _dbContext.Scores.AddAsync(new ScoreDto
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
	}

	public async Task<Score?> GetScore(string checksum = "")
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

	public async Task SetScoreLeaderboardPosition(Beatmap beatmap, Score score)
	{
		//TODO
	}
}