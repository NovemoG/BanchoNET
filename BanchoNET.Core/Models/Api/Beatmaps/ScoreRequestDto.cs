using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ScoreRequestDto
{
    [JsonPropertyName("version_hash")]
    public required string VersionHash { get; set; }
    
    [JsonPropertyName("beatmap_hash")]
    public required string BeatmapHash { get; set; }
    
    [JsonPropertyName("ruleset_id")]
    public int RulesetID { get; set; }
}