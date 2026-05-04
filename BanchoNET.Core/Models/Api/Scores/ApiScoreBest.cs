using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Api.Scores;

public class ApiScoreBest : ApiScoreExtended
{
    public Performance Weight { get; set; }

    public ApiScoreBest(
        ScoreDto score,
        PlayerDto player,
        Beatmap beatmap,
        Beatmapset beatmapset,
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
        ScoreDto score,
        PlayerDto player,
        BeatmapDto beatmap,
        BeatmapsetDto beatmapset,
        int index
    ) : base(score, player, beatmap, beatmapset) {
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