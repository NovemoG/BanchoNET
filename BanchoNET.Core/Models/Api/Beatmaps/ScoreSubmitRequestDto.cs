using BanchoNET.Core.Models.Api.Scores;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ScoreSubmitRequestDto
{
    public int RulesetId { get; set; }
    public bool Passed { get; set; }
    public int TotalScore { get; set; }
    public int TotalScoreWithoutMods { get; set; }
    public double Accuracy { get; set; }
    public int MaxCombo { get; set; }
    public Statistics Statistics { get; set; } = new();
    public MaxStatistics MaximumStatistics { get; set; }
    public int[] Pauses { get; set; } = [];
    public bool Ranked { get; set; }
}