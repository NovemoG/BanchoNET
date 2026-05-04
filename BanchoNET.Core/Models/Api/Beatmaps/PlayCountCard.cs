namespace BanchoNET.Core.Models.Api.Beatmaps;

public class PlayCountCard
{
    public int BeatmapId { get; set; }
    public int Count { get; set; }
    public required PlayCountBeatmap Beatmap { get; set; }
    public required BasicApiBeatmapset Beatmapset { get; set; }
}