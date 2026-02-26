using System.Collections.Concurrent;
using BanchoNET.Core.Models.Beatmaps;
using Novelog.Abstractions;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public class BeatmapStateService(ILogger logger) : StatefulService<int, Beatmap>(logger)
{
    //BeatmapsById already exist in StatefulService
    protected readonly ConcurrentDictionary<string, Beatmap> BeatmapsByMD5 = new();
    protected readonly ConcurrentDictionary<int, BeatmapSet> BeatmapSets = new();

    protected readonly ConcurrentDictionary<string, bool> NotSubmittedBeatmaps = new();
    protected readonly ConcurrentDictionary<string, bool> NeedUpdateBeatmaps = new();
}