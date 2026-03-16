using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface ILazerScoresRepository : IScoresRepository
{
    Task<ApiScore> InsertScore(
        ApiScore score,
        bool isPlayerRestricted,
        string md5,
        int mapId
    );
    
    Task UpdateScoreStatus(ApiScore? score);

    Task<ApiScore?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        int mapId
    );

    Task<ApiScore?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        List<ApiMod> mods,
        int mapId
    );

    Task SetScoreLeaderboardPosition(
        ApiScore score,
        bool withMods,
        int mapId,
        List<ApiMod>? mods = null
    );

    Task<(List<ApiScore>, int, ApiScore?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        List<ApiMod> mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        int mapId
    );
}