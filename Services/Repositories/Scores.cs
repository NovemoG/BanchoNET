using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Scores;
using BanchoNET.Utils;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
    private static bool OrderByPp(GameMode mode) => mode >= GameMode.RelaxStd || AppSettings.SortLeaderboardByPP;
	
    public async Task InsertScore(Score score, string beatmapMD5, Player player)
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
            Perfect = score.Perfect,
            OnlineChecksum = score.ClientChecksum,
            IsRestricted = player.Restricted
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

    public async Task SetScoresStatuses(Score? previousScore, Score? previousWithMods)
    {
        if (previousScore != null)
            await _dbContext.Scores.Where(s => s.Id == previousScore.Id).ExecuteUpdateAsync(s => 
                s.SetProperty(u => u.Status, (int)previousScore.Status));

        if (previousWithMods != null)
            await _dbContext.Scores.Where(s => s.Id == previousWithMods.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.Status, (int)previousWithMods.Status));
    }
    
    public async Task<Score?> GetPlayerBestScoreOnMap(
        Player player,
        string beatmapMD5,
        GameMode mode)
    {
        var score = await _dbContext.Scores.FirstOrDefaultAsync(
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
        var response = await _dbContext.Scores.FirstOrDefaultAsync(
            s => s.PlayerId == player.Id
                 && s.BeatmapMD5 == beatmapMD5
                 && s.Mode == (int)mode
                 && s.Mods == (int)mods
                 && s.Status >= (int)SubmissionStatus.BestWithMods);

        return response == null ? null : new Score(response);
    }
	
    public async Task<ScoreDto?> GetBestBeatmapScore(Beatmap beatmap, GameMode mode)
    {
        return await _dbContext.Scores.Include(s => s.Player)
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
        score.LeaderboardPosition = await _dbContext.Scores
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
		
        var result = await _dbContext.Scores.AsNoTracking()
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
}