using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Scores;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ScoreSubmitRequestDto
{
    [JsonPropertyName("ruleset_id")]
    public int RulesetId { get; set; }
    public bool Passed { get; set; }
    
    [JsonPropertyName("total_score")]
    public int TotalScore { get; set; }
    [JsonPropertyName("total_score_without_mods")]
    public int TotalScoreWithoutMods { get; set; }
    public double Accuracy { get; set; }
    [JsonPropertyName("max_combo")]
    public int MaxCombo { get; set; }
    public required string Rank { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ApiMod>? Mods { get; set; }
    
    public Statistics Statistics { get; set; } = new();
    [JsonPropertyName("maximum_statistics")]
    public MaxStatistics MaximumStatistics { get; set; } = new();
    public int[] Pauses { get; set; } = [];
    public bool Ranked { get; set; }
}