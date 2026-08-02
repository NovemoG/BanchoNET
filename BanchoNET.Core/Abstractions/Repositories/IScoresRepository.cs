using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IScoresRepository
{
    Task<ScoreDto?> GetScore(long id);
    Task RemoveScore(long id);
    Task<bool> ScoreExists(string checksum);
    Task<bool> RecalculateScore(long id);
    Task RecalculateAllScores();
    Task<List<long>> PurgeOldScores();
    
    Task ToggleBeatmapScoresVisibility(int mapId);
    Task ToggleBeatmapScoresVisibility(string md5);
    Task ToggleScoreReplayAvailability(long scoreId);
    
    Task SetBeatmapScoresRankedStatus(int mapId, bool ranked);

    Task<List<ScoreDto>> GetPlayerRecentScores(
        int playerId,
        GameMode mode,
        int start = 0,
        int count = 50
    );

    Task<int> PlayerRecentScoresCount(
        int playerId,
        GameMode mode
    );

    Task<List<ScoreDto>> GetPlayerFirstPlaceScores(
        int playerId,
        GameMode mode,
        int start = 0,
        int count = 50
    );

    Task<int> PlayerFirstPlaceScoresCount(
        int playerId,
        GameMode mode
    );

    Task UpdateScoreStatus(
        long id,
        SubmissionStatus newStatus
    );

    Task<ScoreDto?> GetBestBeatmapScore(
        string md5,
        GameMode mode
    );
    
    Task<ScoreDto?> GetBestBeatmapScore(
        int mapId,
        GameMode mode
    );
    
    Task<List<ScoreDto>> GetBeatmapLeaderboard(
        GameMode mode,
        LeaderboardType type,
        string mods,
        string country,
        HashSet<int> playerIds,
        string md5
    );

    Task<List<ScoreDto>> GetBeatmapLeaderboard(
        GameMode mode,
        LeaderboardType type,
        string mods,
        string country,
        HashSet<int> playerIds,
        int mapId
    );
    
    Task<List<ScoreDto>> GetPlayerBestScores(
        int playerId,
        GameMode mode,
        int offset,
        int limit
    );

    Task<List<ScoreDto>> GetBestScores(
        GameMode mode,
        int skip = 0,
        int count = 50
    );

    Task<List<ScoreDto>> GetRecentScores(
        GameMode mode,
        int skip = 0,
        int count = 50
    );
}