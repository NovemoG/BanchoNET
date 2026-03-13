using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Scores;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class BeatmapScoresResponseDto
{
    public int ScoreCount { get; set; }
    public List<ApiScore> Scores { get; set; } = [];
    
    public ApiScore? UserScore { get; set; }
    [JsonPropertyName("userScore"), JsonInclude]
    private ApiScore? UserScore2 => UserScore;
}