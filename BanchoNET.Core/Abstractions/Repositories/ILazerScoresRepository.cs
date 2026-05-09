using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface ILazerScoresRepository : IScoresRepository
{
    Task<ApiScore> InsertScore(
        ApiScore score,
        string md5,
        int mapId
    );
    
    Task UpdateScoreStatus(ApiScore? score);

    Task<ApiScore?> GetPlayerBestScoreOnMap(
        int playerId,
        GameMode mode,
        Beatmap beatmap
    );

    Task<ApiScore?> GetPlayerBestScoreWithModsOnMap(
        int playerId,
        GameMode mode,
        string mods,
        Beatmap beatmap
    );

    Task SetScoreLeaderboardPosition(
        ApiScore score,
        bool withMods,
        Beatmap beatmap,
        string mods = ""
    );

    Task<(List<ApiScore>, int, ApiScore?)> GetLeaderboardScores(
        LeaderboardType type,
        GameMode mode,
        string mods,
        int playerId,
        string country,
        HashSet<int> friendIds,
        Beatmap beatmap
    );
}