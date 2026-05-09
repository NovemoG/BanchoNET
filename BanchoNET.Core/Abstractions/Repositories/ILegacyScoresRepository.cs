using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface ILegacyScoresRepository : IScoresRepository
{
    Task<Score> InsertScore(
        Score score,
        string md5,
        int mapId
    );
    
    Task<Score?> GetPlayerRecentScore(int playerId);
    Task UpdateScoreStatus(Score? score);
    
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
        string mods,
        string md5
    );

    Task<Score?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        string mods,
        int mapId
    );
    
    Task SetScoreLeaderboardPosition(
        Score score,
        bool withMods,
        string md5,
        string mods = ""
    );

    Task SetScoreLeaderboardPosition(
        Score score,
        bool withMods,
        int mapId,
        string mods = ""
    );
    
    Task<(List<ScoreDto>, Score?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        string mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        string md5
    );

    Task<(List<ScoreDto>, Score?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        string mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        int mapId
    );
}