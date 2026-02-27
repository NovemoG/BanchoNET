using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class ScoresRepository(BanchoDbContext dbContext) : IScoresRepository
{
    private static bool OrderByPp(GameMode mode) => mode >= GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
	
    public async Task<Score> InsertScore(Score score, string beatmapMD5, bool isPlayerRestricted)
    {
        var dbScore = await dbContext.Scores.AddAsync(new ScoreDto
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
            Perfect = score.Perfect,
            OnlineChecksum = score.ClientChecksum,
            IsRestricted = isPlayerRestricted
        });
        await dbContext.SaveChangesAsync();

        score.Id = dbScore.Entity.Id;
        return score;
    }

    public async Task<ScoreDto?> GetScore(long id)
    {
        return await dbContext.Scores.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> ScoreExists(string checksum)
    {
        if (string.IsNullOrEmpty(checksum)) return false;

        return await dbContext.Scores.AnyAsync(s => s.OnlineChecksum == checksum);
    }

    public async Task<Score?> GetPlayerRecentScore(int playerId)
    {
        var score = await dbContext.Scores.OrderByDescending(s => s.PlayTime)
            .FirstOrDefaultAsync(s => s.PlayerId == playerId);

        return score == null ? null : new Score(score);
    }
    
    public async Task<List<ScoreDto>> GetPlayerRecentScores(int playerId, int start, int count = 10)
    {
        return await dbContext.Scores.Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.PlayTime)
            .Skip(start)
            .Take(count)
            .ToListAsync();
    }
    
    public async Task<List<ScoreDto>> GetMultiplayerScores(List<int> playerIds, DateTime finishDate)
    {
        return await dbContext.Scores
            .Where(s => playerIds.Contains(s.PlayerId) && s.PlayTime > finishDate)
            .ToListAsync();
    }

    public async Task UpdateScoreStatus(Score? score)
    {
        if (score == null) return;

        await UpdateScoreStatus(score.Id, score.Status);
    }

    public async Task UpdateScoreStatus(long id, SubmissionStatus newStatus)
    {
        await dbContext.Scores.Where(s => s.Id == id)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.Status, (int)newStatus));
    }
    
    public async Task<Score?> GetPlayerBestScoreOnMap(
        int playerId,
        string beatmapMD5,
        GameMode mode)
    {
        var score = await dbContext.Scores.FirstOrDefaultAsync(
            s => s.PlayerId == playerId
                 && s.BeatmapMD5 == beatmapMD5
                 && s.Mode == (int)mode
                 && s.Status == (int)SubmissionStatus.Best);

        return score == null ? null : new Score(score);
    }
    
    public async Task<Score?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        string beatmapMD5,
        GameMode mode,
        Mods mods)
    {
        var score = await dbContext.Scores.FirstOrDefaultAsync(
            s => s.PlayerId == playerId
                 && s.BeatmapMD5 == beatmapMD5
                 && s.Mode == (int)mode
                 && s.Mods == (int)mods
                 && s.Status >= (int)SubmissionStatus.BestWithMods);

        return score == null ? null : new Score(score);
    }
	
    public async Task<ScoreDto?> GetBestBeatmapScore(string beatmapMD5, GameMode mode)
    {
        return await dbContext.Scores
            .Include(s => s.Player)
            .Where(s => s.BeatmapMD5 == beatmapMD5
                        && s.Mode == (int)mode
                        && s.Status == (int)SubmissionStatus.Best
                        && (s.Player.Privileges & 1) == 1
                        && !s.IsRestricted)
            .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.Score)
            .FirstOrDefaultAsync();
    }
	
    public async Task SetScoreLeaderboardPosition(
        string beatmapMD5,
        Score score,
        bool withMods,
        Mods mods = Mods.None)
    {
        score.LeaderboardPosition = await dbContext.Scores
            .Include(s => s.Player)
            .Where(s => s.BeatmapMD5 == beatmapMD5 
                        && s.Mode == (int)score.Mode
                        && (withMods
                            ? s.Status >= (int)SubmissionStatus.BestWithMods
                            : s.Status == (int)SubmissionStatus.Best)
                        && (!withMods || s.Mods == (int)mods) 
                        && (s.Player.Privileges & 1) == 1 
                        && !s.IsRestricted
                        && (OrderByPp(score.Mode)
                            ? score.PP < s.PP
                            : score.TotalScore < s.Score))
            .CountAsync() + 1;
    }
	
    public async Task<List<ScoreDto>> GetBeatmapLeaderboard(
        string beatmapMD5,
        GameMode mode,
        LeaderboardType type,
        Mods mods,
        User player
    ) {
        var isCountry = type == LeaderboardType.Country;
        var countryCode = player.Geoloc.Country.Acronym;
		
        var withMods = type is LeaderboardType.Mods or LeaderboardType.CountryMods or LeaderboardType.FriendsMods;
		
        var withFriendsList = type == LeaderboardType.Friends;
        var friendIds = withFriendsList ? player.Friends.ToHashSet() : [];
		
        var result = await dbContext.Scores.AsNoTracking()
             .Include(s => s.Player)
             .Where(s => s.BeatmapMD5 == beatmapMD5 
                         && s.Mode == (int)mode 
                         && (withMods
                             ? s.Status >= (int)SubmissionStatus.BestWithMods
                             : s.Status == (int)SubmissionStatus.Best) 
                         && (s.Player.Privileges & 1) == 1 
                         && !s.IsRestricted
                         && (!withMods || s.Mods == (int)mods) 
                         && (!isCountry || s.Player.Country == countryCode) 
                         && (!withFriendsList || friendIds.Contains(s.PlayerId)))
             .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.Score)
             .Take(AppSettings.ScoresOnLeaderboard)
             .ToListAsync();
        
        /*TODO best we've managed to squeeze out of our brains is <1s with 5mln scores
               we're not knowledgeable enough to make it faster ~Cossin & foksurek*/
        
        return result;
    }

    public async Task ToggleBeatmapScoresVisibility(string beatmapMD5, bool visible)
    {
        await dbContext.Scores.Where(s => s.BeatmapMD5 == beatmapMD5)
            .ExecuteUpdateAsync(p => p.SetProperty(s => s.IsRestricted, visible));
    }
    
    /// <summary>
    /// Deletes all scores with status not flagged as best older than 2 days from when the method was used.
    /// </summary>
    /// <returns>List of IDs of scores that were affected.</returns>
    public async Task<List<long>> DeleteOldScores(short differenceInHours = 48)
    {
        //TODO maybe instead of deleting just move to other table so we can keep the data?
        var date = DateTime.UtcNow - TimeSpan.FromHours(differenceInHours);

        // Saving IDs of scores that are not failed (we don't store failed scores replays)
        var scoreIds = await dbContext.Scores
            .Where(s => s.PlayTime < date
                        && s.Status == (int)SubmissionStatus.Submitted)
            .Select(s => s.Id)
            .ToListAsync();
        
        // Deleting scores
#pragma warning disable EF1002
        var affected = await dbContext.Database.ExecuteSqlRawAsync(
            $"DELETE FROM Scores WHERE PlayTime < TIMESTAMP(\"{date:yyyy-MM-dd}\", \"{date:HH:mm:ss}\") " +
            $"AND Status < {(int)SubmissionStatus.BestWithMods}");
#pragma warning restore EF1002
        Console.WriteLine($"[BackgroundTasks] Deleted {affected} scores.");
        
        return scoreIds;
    }
}