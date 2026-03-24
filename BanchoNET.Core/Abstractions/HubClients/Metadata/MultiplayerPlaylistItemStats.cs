using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Metadata;

[MessagePackObject]
[Serializable]
public class MultiplayerPlaylistItemStats
{
    public const int TOTAL_SCORE_DISTRIBUTION_BINS = 13;
    
    [Key(0)]
    public long PlaylistId { get; set; }
    
    [Key(1)]
    public long[] TotalScoreDistribution { get; set; } = new long[TOTAL_SCORE_DISTRIBUTION_BINS];
    
    [Key(2)]
    public long CumulativeScore { get; set; }
    
    [Key(3)]
    public ulong LastProcessedScoreId { get; set; }
}