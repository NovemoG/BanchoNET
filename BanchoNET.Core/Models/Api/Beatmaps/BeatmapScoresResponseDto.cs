using BanchoNET.Core.Models.Api.Scores;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class BeatmapScoresResponseDto
{
    public int ScoreCount { get; set; }
    public ApiScore[] Scores { get; set; } = [];
    //TODO also userScore
    public ApiScore? UserScore { get; set; }
}