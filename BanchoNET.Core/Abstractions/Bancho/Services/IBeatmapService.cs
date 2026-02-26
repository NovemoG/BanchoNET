using BanchoNET.Core.Models.Beatmaps;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public interface IBeatmapService
{
    void InsertBeatmapSet(BeatmapSet set);
    void InsertNeedsUpdateBeatmap(string md5);
    bool BeatmapNeedsUpdate(string md5);
    
    Beatmap? GetBeatmap(string md5);
    Beatmap? GetBeatmap(int mapId);
    BeatmapSet? GetBeatmapSet(int setId);
}