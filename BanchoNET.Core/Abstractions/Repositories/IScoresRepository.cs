using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IScoresRepository
{
    Task<ScoreLeaderboard> GetLeaderboardScores(
        int mapId,
        GameMode mode,
        LeaderboardType type,
        string mods,
        int playerId,
        string country,
        HashSet<int> friends,
        HashSet<int> teammates,
        bool representedByPp,
        LeaderboardSort sort,
        int limit
    );

    Task<ScoreLeaderboardPreferences> GetLeaderboardPreferences(int playerId);
    Task<ScoreDto?> GetPlayerRecentScore(int playerId);
    Task<ScoreDto?> GetPlayerBestScoreOnMap(int playerId, GameMode mode, int mapId, bool performancePoints, string? mods = null);
    Task<int> GetLeaderboardPosition(ScoreDto score, bool performancePoints, LeaderboardSort sort, string? mods = null);

    Task<ScoreDto?> GetScore(long id);
    Task<bool> GetLeaderboardPerformancePoints(int playerId);
    Task<List<long>> PurgeOldScores();
    
    Task ToggleBeatmapScoresVisibility(int mapId);
    Task ToggleBeatmapScoresVisibility(string md5);
    
    Task SetBeatmapScoresRankedStatus(int mapId, bool ranked);

    Task<List<ScoreDto>> GetPlayerRecentScores(
        int playerId,
        GameMode mode,
        int start = 0,
        int count = 50,
        bool failed = false
    );

    Task<int> PlayerRecentScoresCount(
        int playerId,
        GameMode mode,
        bool failed = false
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

    Task<ScoreDto?> GetBestBeatmapScore(
        string md5,
        GameMode mode
    );
    
    Task<ScoreDto?> GetBestBeatmapScore(
        int mapId,
        GameMode mode
    );
    
    Task<List<ScoreDto>> GetPlayerBestScores(
        int playerId,
        GameMode mode,
        int offset,
        int limit
    );
    
    Task<List<ScoreDto>> GetPlayerBestScoresWithMods(
        int playerId,
        GameMode mode,
        int offset,
        int limit,
        string[] mods,
        bool matchExactly
    );

    /// <summary>
    /// How many scores <see cref="GetPlayerBestScoresWithMods"/> can page through.
    /// </summary>
    Task<int> GetPlayerBestScoresWithModsCount(
        int playerId,
        GameMode mode,
        string[] mods,
        bool matchExactly
    );

    Task<List<ScoreDto>> GetBestScores(
        GameMode mode,
        int skip = 0,
        int count = 50
    );

    /// <summary>
    /// How many scores <see cref="GetBestScores"/> can page through.
    /// </summary>
    Task<int> GetBestScoresCount(
        GameMode mode
    );

    Task<List<ScoreDto>> GetRecentScores(
        GameMode mode,
        int skip = 0,
        int count = 50
    );
}