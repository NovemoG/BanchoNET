using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IScoresRepository
{
    Task<ScoreDto?> GetScore(long id);
    Task<bool> ScoreExists(string checksum);
    Task<Score?> GetPlayerRecentScore(int playerId);
    Task UpdateScoreStatus(Score? score);
    Task<List<long>> DeleteOldScores(short differenceInHours = 48);
    
    Task ToggleBeatmapScoresVisibility(int mapId, bool visible);
    Task ToggleBeatmapScoresVisibility(string md5, bool visible);

    Task<Score> InsertScore(
        Score score,
        bool isPlayerRestricted,
        string md5,
        int mapId
    );
    Task<ApiScore> InsertScore(
        ApiScore score,
        bool isPlayerRestricted,
        string md5,
        int mapId
    );

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

    Task<Score?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        string md5
    );

    Task<Score?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        int mapId
    );

    Task<Score?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        LegacyMods mods,
        string md5
    );

    Task<Score?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        LegacyMods mods,
        int mapId
    );

    Task<ScoreDto?> GetBestBeatmapScore(
        string md5,
        GameMode mode
    );
    
    Task<ScoreDto?> GetBestBeatmapScore(
        int mapId,
        GameMode mode
    );

    Task SetScoreLeaderboardPosition(
        Score score,
        bool withMods,
        string md5,
        LegacyMods mods = LegacyMods.None
    );

    Task SetScoreLeaderboardPosition(
        Score score,
        bool withMods,
        int mapId,
        LegacyMods mods = LegacyMods.None
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

    Task<(List<ScoreDto>, Score?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        LegacyMods mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        string md5
    );

    Task<(List<ScoreDto>, Score?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        LegacyMods mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        int mapId
    );
}