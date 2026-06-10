using System.Collections.Concurrent;
using BanchoNET.Core.Models.Beatmaps;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public class BeatmapStateService(ILogger logger) : StatefulService<int, Beatmap>(logger)
{
    //BeatmapsById already exist in StatefulService
    protected readonly ConcurrentDictionary<string, int> BeatmapsByMD5 = new(StringComparer.OrdinalIgnoreCase);
    protected readonly ConcurrentDictionary<int, Beatmapset> BeatmapSets = new();
    
    protected readonly ConcurrentDictionary<string, bool> NeedUpdateBeatmaps = new(StringComparer.OrdinalIgnoreCase);
}