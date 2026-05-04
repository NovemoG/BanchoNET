using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Api.Scores;

public class ApiScoreExtended : ApiScore
{
    public BasicApiBeatmap Beatmap { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BasicApiBeatmapset? Beatmapset { get; set; }

    public ApiScoreExtended(
        ScoreDto score,
        PlayerDto player,
        Beatmap beatmap,
        Beatmapset? beatmapset
    ) : base(score, player, beatmap) {
        Beatmap = new BasicApiBeatmap(beatmap);
        
        if (beatmapset != null)
            Beatmapset = new BasicApiBeatmapset(beatmapset);
    }

    public ApiScoreExtended(
        ScoreDto score,
        PlayerDto player,
        BeatmapDto beatmap,
        BeatmapsetDto? beatmapset
    ) : base(score, player, beatmap) {
        Beatmap = new BasicApiBeatmap(beatmap);
        
        if (beatmapset != null)
            Beatmapset = new BasicApiBeatmapset(beatmapset);
    }

    public ApiScoreExtended(
        ScoreDto score,
        PlayerDto player,
        BeatmapDto beatmap
    ) : base(score, player, beatmap) {
        Beatmap = new BasicApiBeatmap(beatmap);
    }
}