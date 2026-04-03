using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingUser
{
    [Key(0)]
    public required int UserId { get; set; }
    
    [Key(1)]
    public int? Placement { get; set; }
    
    [Key(2)]
    public int Points { get; set; }
    
    [Key(3)]
    public MatchmakingRoundList Rounds { get; set; } = new();
    
    [Key(4)]
    public DateTimeOffset? AbandonedAt { get; set; }
}