using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Scores;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;

[Serializable]
[MessagePackObject]
[method: JsonConstructor]
[method: SerializationConstructor]
public class FrameHeader(
    long totalScore,
    double accuracy,
    int combo,
    int maxCombo,
    Dictionary<HitResult, int> statistics,
    ScoreProcessorStatistics scoreProcessorStatistics,
    DateTimeOffset receivedTime
) {
    [Key(0)]
    public long TotalScore { get; set; } = totalScore;

    [Key(1)]
    public double Accuracy { get; set; } = accuracy;

    [Key(2)]
    public int Combo { get; set; } = combo;

    [Key(3)]
    public int MaxCombo { get; set; } = maxCombo;

    [Key(4)]
    public Dictionary<HitResult, int> Statistics { get; set; } = statistics;

    [Key(5)]
    public ScoreProcessorStatistics ScoreProcessorStatistics { get; set; } = scoreProcessorStatistics;

    [Key(6)]
    public DateTimeOffset ReceivedTime { get; set; } = receivedTime;

    [Key(7)]
    public ApiMod[]? Mods { get; set; }
}