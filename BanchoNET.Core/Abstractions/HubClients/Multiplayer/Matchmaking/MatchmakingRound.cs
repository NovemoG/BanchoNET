using BanchoNET.Core.Models.Scores;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingRound
{
    [Key(0)]
    public required int Round { get; set; }
    
    [Key(1)]
    public int Placement { get; set; }
    
    [Key(2)]
    public long TotalScore { get; set; }
    
    [Key(3)]
    public double Accuracy { get; set; }
    
    [Key(4)]
    public int MaxCombo { get; set; }
    
    [Key(5)]
    public IDictionary<HitResult, int> Statistics { get; set; } = new Dictionary<HitResult, int>();
}