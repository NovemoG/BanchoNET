using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Models.Api.Scores;

public class ApiScoreBest : ApiScoreExtended
{
    public Performance Weight { get; set; }
    
    [JsonConstructor]
    public ApiScoreBest() { }

    public ApiScoreBest(
        Score score,
        Players.Player player,
        Beatmap beatmap,
        BeatmapSet beatmapset,
        int index
    ) : base(score, player, beatmap, beatmapset) {
        var weight = MathF.Pow(0.95f, index);

        Weight = new Performance
        {
            Percentage = weight * 100d,
            Pp = Pp * weight
        };
    }

    public ApiScoreBest(
        ScoreDto scoreDto,
        PlayerDto player,
        Beatmap beatmap,
        BeatmapSet beatmapset,
        int index
    ) : base(scoreDto, player, beatmap, beatmapset) {
        var weight = MathF.Pow(0.95f, index);
        
        Weight = new Performance
        {
            Percentage = weight * 100d,
            Pp = Pp * weight
        };
    }
}

public class Performance
{
    public double Percentage { get; set; }
    public double Pp { get; set; }
}