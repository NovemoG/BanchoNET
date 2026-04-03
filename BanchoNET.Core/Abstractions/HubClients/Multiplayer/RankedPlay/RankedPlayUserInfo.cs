using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;

[Serializable]
[MessagePackObject]
public class RankedPlayUserInfo
{
    [Key(0)]
    public required int Rating { get; set; }
    
    [Key(1)]
    public int Life { get; set; } = 1_000_000;
    
    [Key(2)]
    public List<RankedPlayCardItem> Hand { get; set; } = [];
    
    [Key(3)]
    public int RatingAfter { get; set; }
    
    [Key(4)]
    public RankedPlayDamageInfo? DamageInfo;
}