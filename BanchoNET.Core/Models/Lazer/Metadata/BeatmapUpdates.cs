using MessagePack;

namespace BanchoNET.Core.Models.Lazer.Metadata;

[MessagePackObject]
[Serializable]
public class BeatmapUpdates(int[] beatmapSetIDs, int lastProcessedQueueID)
{
    [Key(0)]
    public int[] BeatmapSetIDs { get; set; } = beatmapSetIDs;

    [Key(1)]
    public int LastProcessedQueueID { get; set; } = lastProcessedQueueID;
}