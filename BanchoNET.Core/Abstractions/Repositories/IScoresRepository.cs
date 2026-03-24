using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IScoresRepository
{
    Task<ScoreDto?> GetScore(long id);
    Task<bool> ScoreExists(string checksum);
    Task<List<long>> DeleteOldScores(short differenceInHours = 48);
    
    Task ToggleBeatmapScoresVisibility(int mapId, bool visible);
    Task ToggleBeatmapScoresVisibility(string md5, bool visible);

    Task<List<ScoreDto>> GetPlayerRecentScores(
        int playerId,
        int start,
        int count = 10
    );

    Task<List<ScoreDto>> GetMultiplayerScores(
        List<int> playerId,
        DateTime finishDate
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
        LegacyMods mods,
        string country,
        HashSet<int> playerIds,
        string md5
    );

    Task<List<ScoreDto>> GetBeatmapLeaderboard(
        GameMode mode,
        LeaderboardType type,
        LegacyMods mods,
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
}