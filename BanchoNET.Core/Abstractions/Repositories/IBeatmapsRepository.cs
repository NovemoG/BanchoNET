using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IBeatmapsRepository
{
    Task<List<BeatmapsetDto>> GetRandomBeatmaps();
    
    Task<BeatmapDto?> GetBeatmap(
        int mapId
    );
    
    Task<BeatmapDto?> GetBeatmap(
        string checksum
    );
    
    Task<BeatmapsetDto?> GetBeatmapset(
        int setId
    );

    Task<BeatmapsetDto?> GetBeatmapsetFull(
        int setId
    );

    Task<List<BeatmapPlays>> FetchPlayerPlaycount(
        int setId,
        int playerId
    );
    
    Task<bool> FetchPlayerFavorited(
        int setId,
        int playerId
    );

    Task<List<int>> GetPlayerFavoriteBeatmapsets(
        int playerId,
        int offset = 0,
        int count = 7
    );

    Task<List<BeatmapPlays>> GetPlayerMostPlayedBeatmaps(
        int playerId,
        int offset = 0,
        int count = 6
    );

    Task UpdateBeatmapPlayCount(
        Beatmap beatmap,
        int playerId
    );

    Task UpdateBeatmapsetFavoriteCount(
        Beatmapset beatmapset,
        int playerId,
        bool add
    );

    Task UpdateBeatmapMaxStatistics(
        Beatmap beatmap,
        Dictionary<HitResult, int> statistics
    );

    Task UpdateBeatmapFailTimes(
        int beatmapId,
        int index,
        bool isFail
    );

    Task<int> UpdateBeatmapStatus(
        int mapId,
        BeatmapStatus targetStatus
    );

    Task<int> UpdateBeatmapsetStatus(
        int setId,
        BeatmapStatus targetStatus
    );

    Task InsertBeatmapset(Beatmapset set);
}