using BanchoNET.Objects.Beatmaps;

namespace BanchoNET.Abstractions.Repositories;

public interface IBeatmapsRepository
{
    Task<Beatmap?> GetBeatmap(int mapId, int setId = -1);
    Task<Beatmap?> GetBeatmap(string beatmapMD5, int setId = -1);
    Task<BeatmapSet?> GetBeatmapSet(int setId, int mapId = -1);

    Task UpdateBeatmapSet(int setId);
    Task UpdateBeatmapPlayCount(Beatmap beatmap);
    Task<int> UpdateBeatmapStatus(BeatmapStatus targetStatus, int mapId);
    Task<int> UpdateBeatmapSetStatus(BeatmapStatus targetStatus, int setId);

    Task InsertBeatmapSet(BeatmapSet set);
}