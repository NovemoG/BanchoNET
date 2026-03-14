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
        
        BeatmapSets.AddOrUpdate(set.Id, set, (prevKey, prevSet) =>
        {
            foreach (var map in prevSet.Beatmaps)
                BeatmapsByMD5.TryRemove(map.MD5, out _);
            
            return set;
        });
        
        foreach (var beatmap in set.Beatmaps)
        {
            BeatmapsByMD5.TryAdd(beatmap.MD5, beatmap.Id);
            Items.AddOrUpdate(beatmap.Id, beatmap, (_, prev) => UpdateStatus(beatmap, prev));
        }
    }

    public bool BeatmapNeedsUpdate(
        string md5
    ) {
        return NeedUpdateBeatmaps.ContainsKey(md5);
    }

    public Beatmap? GetBeatmap(
        string beatmapMD5
    ) {
        if (!BeatmapsByMD5.TryGetValue(beatmapMD5, out var id))
            return null;

        if (!TryGet(id, out var beatmap))
        {
            BeatmapsByMD5.TryRemove(beatmapMD5, out _);
            return null;
        }
        
        if (string.Equals(beatmap!.MD5, beatmapMD5, StringComparison.OrdinalIgnoreCase))
            return beatmap;
        
        BeatmapsByMD5.TryRemove(beatmapMD5, out _);
        NeedUpdateBeatmaps.TryAdd(beatmapMD5, false);
        return null;
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