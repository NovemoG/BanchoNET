using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Scores;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class BeatmapScoresResponseDto
{
    public int ScoreCount { get; set; }
    public List<ApiScore> Scores { get; set; } = [];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserScore? UserScore { get; set; }
    [JsonPropertyName("userScore"),JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private UserScore? UserScore2 => UserScore;
}