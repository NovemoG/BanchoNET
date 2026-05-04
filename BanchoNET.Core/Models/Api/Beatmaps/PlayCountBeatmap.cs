using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class PlayCountBeatmap
{
    public int BeatmapsetId { get; set; }
    public float DifficultyRating { get; set; }
    public int Id { get; set; }
    public string Mode { get; set; } = "osu";
    public string Status { get; set; } = string.Empty;
    public int TotalLength { get; set; }
    public int UserId { get; set; }
    public string Version { get; set; } = string.Empty;
    
    [JsonConstructor]
    public PlayCountBeatmap() { }
    
    public PlayCountBeatmap(
        BeatmapDto beatmap
    ) {
        BeatmapsetId = beatmap.SetId;
        DifficultyRating = beatmap.StarRating;
        Id = beatmap.Id;
        Mode = EnumExtensions.FromModeMap[beatmap.Mode];
        Status = beatmap.Status.ToApiBeatmapStatus();
        TotalLength = beatmap.TotalLength;
        UserId = 1;
        Version = beatmap.Version;
    }
}