using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Spectator;

[Serializable]
[MessagePackObject]
public class ScoreProcessorStatistics
{
    [Key(0)]
    public double BaseScore { get; set; }
    
    [Key(1)]
    public double MaximumBaseScore { get; set; }
    
    [Key(2)]
    public int AccuracyJudgementCount { get; set; }
    
    [Key(3)]
    public double ComboPortion { get; set; }
    
    [Key(4)]
    public double BonusPortion { get; set; }
}