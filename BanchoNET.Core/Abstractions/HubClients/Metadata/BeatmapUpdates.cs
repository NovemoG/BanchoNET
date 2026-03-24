using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Metadata;

[MessagePackObject]
[Serializable]
public class BeatmapUpdates(int[] beatmapSetIDs, int lastProcessedQueueID)
{
    [Key(0)]
    public int[] BeatmapSetIDs { get; set; } = beatmapSetIDs;

    [Key(1)]
    public int LastProcessedQueueID { get; set; } = lastProcessedQueueID;
}