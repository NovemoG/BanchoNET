using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Abstractions.Repositories;

public interface IBeatmapsRepository
{
    Task<List<BeatmapsetDto>> GetRandomBeatmaps();
    Task<Beatmap?> GetBeatmap(int mapId, int setId = -1);
    Task<Beatmap?> GetBeatmap(string beatmapMD5, int setId = -1);
    Task<BeatmapSet?> GetBeatmapSet(int setId, int mapId = -1, bool recheckApi = false);
    Task<List<Beatmap>> GetBeatmaps(int[] beatmapIds);
    
    Task UpdateBeatmapSet(int setId);
    Task UpdateBeatmapPlayCount(Beatmap beatmap);
    Task<int> UpdateBeatmapStatus(BeatmapStatus targetStatus, int mapId);
    Task<int> UpdateBeatmapSetStatus(BeatmapStatus targetStatus, int setId);

    Task InsertBeatmapSet(BeatmapSet set);
}