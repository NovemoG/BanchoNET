using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Metadata;

[Serializable]
[MessagePackObject]
public class MultiplayerRoomScoreSetEvent
{
    [Key(0)]
    public long RoomID { get; set; }
    
    [Key(1)]
    public long PlaylistItemID { get; set; }
    
    [Key(2)]
    public long ScoreID { get; set; }
    
    [Key(3)]
    public int UserID { get; set; }
    
    [Key(4)]
    public long TotalScore { get; set; }
    
    [Key(5)]
    public int? NewRank { get; set; }
}