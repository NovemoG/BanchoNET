using BanchoNET.Core.Models.Beatmaps;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public interface IBeatmapService
{
    void InsertBeatmapset(Beatmapset set);
    bool BeatmapNeedsUpdate(string md5);
    
    Beatmap? GetBeatmap(string md5);
    Beatmap? GetBeatmap(int mapId);
    Beatmapset? GetBeatmapset(int setId);
}