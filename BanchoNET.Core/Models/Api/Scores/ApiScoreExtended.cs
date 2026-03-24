using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Models.Api.Scores;

public class ApiScoreExtended : ApiScore
{
    public BasicApiBeatmap Beatmap { get; set; }
    public BasicApiBeatmapset Beatmapset { get; set; }
    
    [JsonConstructor]
    public ApiScoreExtended() { }

    public ApiScoreExtended(
        Score score,
        Players.Player player,
        Beatmap beatmap,
        BeatmapSet beatmapset
    ) : base(score, player, beatmap) {
        Beatmap = new BasicApiBeatmap(beatmap);
        Beatmapset = new BasicApiBeatmapset(beatmapset, beatmap);
    }

    public ApiScoreExtended(
        ScoreDto scoreDto,
        PlayerDto player,
        Beatmap beatmap,
        BeatmapSet beatmapset
    ) : base(scoreDto, player, beatmap) {
        Beatmap = new BasicApiBeatmap(beatmap);
        Beatmapset = new BasicApiBeatmapset(beatmapset, beatmap);
    }
}