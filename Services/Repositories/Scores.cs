using BanchoNET.Models;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Scores;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services.Repositories;

public class ScoresRepository(BanchoDbContext dbContext)
{
    private static bool OrderByPp(GameMode mode) => mode >= GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
	
    public async Task<Score> InsertScore(Score score, string beatmapMD5, Player player)
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
            IsRestricted = player.Restricted
        });
        await dbContext.SaveChangesAsync();

        score.Id = dbScore.Entity.Id;

        return new Score(dbScore.Entity);
    }

    public async Task<ScoreDto?> GetScore(string checksum)
    {
        if (!string.IsNullOrEmpty(checksum))
            return await dbContext.Scores.FirstOrDefaultAsync(s => s.OnlineChecksum == checksum);

        return null;
    }

    public async Task<Score?> GetPlayerRecentScore(int playerId)
    {
        var score = await dbContext.Scores.OrderByDescending(s => s.PlayTime)
            .FirstOrDefaultAsync(s => s.PlayerId == playerId);

        return score == null ? null : new Score(score);
    }
    
    public async Task<List<ScoreDto>> GetPlayersRecentScores(IEnumerable<int> playerIds, DateTime finishDate)
    {
        var date = finishDate - TimeSpan.FromSeconds(10);
        
        //TODO idk why this query does not return any results; raw sql works fine but with ef does not
        var scores = await dbContext.Scores
            .FromSqlRaw($"SELECT * FROM Scores WHERE PlayerId IN ({string.Join(", ", playerIds)}) AND PlayTime > TIMESTAMP(\"{date:yyyy-MM-dd}\", \"{date:HH:mm:ss}\")")
            .ToListAsync();
         /*var scores = await dbContext.Scores
            .Where(s => playerIds.Contains(s.PlayerId) && s.PlayTime > date)
            .ToListAsync();*/
        
        Console.WriteLine($"Scores: {scores.Count}, date: {date}");

        return scores;
    }

    public async Task SetScoresStatuses(Score? previousScore, Score? previousWithMods)
    {
        if (previousScore != null)
            await dbContext.Scores.Where(s => s.Id == previousScore.Id).ExecuteUpdateAsync(s => 
                s.SetProperty(u => u.Status, (int)previousScore.Status));

        if (previousWithMods != null)
            await dbContext.Scores.Where(s => s.Id == previousWithMods.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.Status, (int)previousWithMods.Status));
    }
    
    public async Task<Score?> GetPlayerBestScoreOnMap(
        Player player,
        string beatmapMD5,
        GameMode mode)
    {
        var score = await dbContext.Scores.FirstOrDefaultAsync(
            s => s.PlayerId == player.Id
                 && s.BeatmapMD5 == beatmapMD5
                 && s.Mode == (int)mode
                 && s.Status == (int)SubmissionStatus.Best);

        return score == null ? null : new Score(score);
    }
    
    public async Task<Score?> GetPlayerBestScoreWithModsOnMap(
        Player player,
        string beatmapMD5,
        GameMode mode,
        Mods mods)
    {
        var response = await dbContext.Scores.FirstOrDefaultAsync(
            s => s.PlayerId == player.Id
                 && s.BeatmapMD5 == beatmapMD5
                 && s.Mode == (int)mode
                 && s.Mods == (int)mods
                 && s.Status >= (int)SubmissionStatus.BestWithMods);

        return response == null ? null : new Score(response);
    }
	
    public async Task<ScoreDto?> GetBestBeatmapScore(Beatmap beatmap, GameMode mode)
    {
        return await dbContext.Scores.Include(s => s.Player)
            .Where(s => s.BeatmapMD5 == beatmap.MD5
                        && s.Mode == (int)mode
                        && s.Status == (int)SubmissionStatus.Best
                        && (s.Player.Privileges & 1) == 1)
            .OrderByDescending(s => OrderByPp(mode) ? s.PP : s.Score)
            .FirstOrDefaultAsync();
    }
	
    public async Task SetScoreLeaderboardPosition(
        Beatmap beatmap,
        Score score,
        bool withMods,
        Mods mods = Mods.None)
    {
        score.LeaderboardPosition = await dbContext.Scores
            .Include(s => s.Player)
            .Where(s => s.BeatmapMD5 == beatmap.MD5 
                        && s.Mode == (int)score.Mode
                        && (withMods
                            ? s.Status >= (int)SubmissionStatus.BestWithMods
                            : s.Status == (int)SubmissionStatus.Best)
                        && (!withMods || s.Mods == (int)mods) 
                        && (s.Player.Privileges & 1) == 1 
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
        Player player)
    {
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
    
    /// <summary>
    /// Deletes all scores with status not flagged as best older than 2 days from when the method was used.
    /// </summary>
    /// <returns>List of IDs of scores that were affected.</returns>
    public async Task<List<long>> DeleteOldScores()
    {
        var date = DateTime.Now - TimeSpan.FromDays(2);

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