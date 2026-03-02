using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Beatmaps;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Services;

public class BeatmapService(
    ILogger logger
) : BeatmapStateService(logger), IBeatmapService
{
    public void InsertBeatmapSet(
        BeatmapSet set
    ) {
        //no need to verify since we're only adding api-validated sets
        Logger.LogDebug($"Caching beatmap set with id: {set.Id}");
        
        BeatmapSets.AddOrUpdate(set.Id, set, (_, _) => set);
        
        foreach (var beatmap in set.Beatmaps)
        {
            BeatmapsByMD5.AddOrUpdate(beatmap.MD5, beatmap, (_, prev) => UpdateStatus(beatmap, prev));
            Items.AddOrUpdate(beatmap.Id, beatmap, (_, prev) => UpdateStatus(beatmap, prev));
        }
    }

    public void InsertNeedsUpdateBeatmap(
        string md5
    ) {
        Logger.LogDebug($"Caching beatmap which needs update with MD5: {md5}");

        NeedUpdateBeatmaps.TryAdd(md5, false);
    }

    public bool BeatmapNeedsUpdate(
        string md5
    ) {
        return NeedUpdateBeatmaps.ContainsKey(md5);
    }

    public Beatmap? GetBeatmap(
        string beatmapMD5
    ) {
        return BeatmapsByMD5.TryGetValue(beatmapMD5, out var cachedBeatmap) ? cachedBeatmap : null;
    }

    public Beatmap? GetBeatmap(
        int mapId
    ) {
        return TryGet(mapId, out var cachedBeatmap) ? cachedBeatmap : null;
    }

    public BeatmapSet? GetBeatmapSet(
        int setId
    ) {
        return BeatmapSets.TryGetValue(setId, out var beatmapSet) ? beatmapSet : null;
    }

    private static Beatmap UpdateStatus(
        Beatmap currentBeatmap,
        Beatmap prevBeatmap
    ) {
        if (!currentBeatmap.IsRankedOfficially)
        {
            currentBeatmap.Status = prevBeatmap.Status;
            currentBeatmap.ApiChecks = prevBeatmap.ApiChecks;
            currentBeatmap.NextApiCheck = prevBeatmap.NextApiCheck;
        }

        return currentBeatmap;
    }
}