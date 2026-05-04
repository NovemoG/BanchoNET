using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Beatmaps;

namespace BanchoNET.Core.Abstractions.Services;

public interface IBeatmapHandler
{
    Task<List<ApiBeatmapsetFull>> GetRandomBeatmaps();
    
    Task<bool> CheckIfMapExistsOnBanchoByFilename(string filename);
    Task<bool> EnsureLocalBeatmapFile(int beatmapId, string beatmapMD5);
    
    Task FetchPlayerPlaycount(
        ApiBeatmapsetFull beatmapset,
        int playerId
    );
    
    Task FetchPlayerFavorited(
        ApiBeatmapsetFull beatmapset,
        int playerId
    );
    
    Task<Beatmap?> GetBeatmap(int mapId, int setId = -1);
    Task<Beatmap?> GetBeatmap(string beatmapMD5, int setId = -1);
    Task<Beatmapset?> GetBeatmapset(int setId, int mapId = -1, bool recheckApi = false);
    Task<List<Beatmap>> GetBeatmaps(int[] beatmapIds);

    Task<ApiBeatmapsetFull?> GetBeatmapsetFromApiOrCached(
        int beatmapsetId,
        bool withAllData = true
    );

    Task<ApiBeatmapsetFull?> GetBeatmapsetByMapIdFromApiOrCached(
        int mapId,
        bool withAllData = true
    );
}