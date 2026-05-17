using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Beatmaps;

namespace BanchoNET.Infrastructure.Bancho.Services;

public class BeatmapService(
    ILogger logger
) : BeatmapStateService(logger), IBeatmapService
{
    public void InsertBeatmapset(
        Beatmapset set
    ) {
        //no need to verify since we're only adding api-validated sets
        Logger.LogDebug($"Caching beatmap set with id: {set.Id}");
        
        BeatmapSets.AddOrUpdate(set.Id, set, (prevKey, prevSet) =>
        {
            foreach (var beatmap in prevSet.Beatmaps)
                BeatmapsByMD5.TryRemove(beatmap.Checksum, out _);
            
            return set;
        });
        
        foreach (var beatmap in set.Beatmaps)
        {
            BeatmapsByMD5.TryAdd(beatmap.Checksum, beatmap.Id);
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
        
        if (string.Equals(beatmap!.Checksum, beatmapMD5, StringComparison.OrdinalIgnoreCase))
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

    public Beatmapset? GetBeatmapset(
        int setId
    ) {
        return BeatmapSets.TryGetValue(setId, out var beatmapSet) ? beatmapSet : null;
    }

    private static Beatmap UpdateStatus(
        Beatmap currentBeatmap,
        Beatmap prevBeatmap
    ) {
        var set = currentBeatmap.Set;
        if (!set.IsRankedOfficially)
        {
            currentBeatmap.Status = prevBeatmap.Status;
            set.ApiChecks = prevBeatmap.Set.ApiChecks;
            set.NextApiCheck = prevBeatmap.Set.NextApiCheck;
        }

        return currentBeatmap;
    }
}