using BanchoNET.Objects.Beatmaps;

namespace BanchoNET.Abstractions.Services;

public interface IBeatmapHandler
{
    Task<bool> CheckIfMapExistsOnBanchoByFilename(string filename);
    Task<bool> EnsureLocalBeatmapFile(int beatmapId, string beatmapMD5);
    Task<Beatmap?> GetBeatmapFromApi(string beatmapMD5 = "", int mapId = -1);
    Task<BeatmapSet?> GetBeatmapSetFromApi(int setId);
}