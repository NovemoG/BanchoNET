using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class BeatmapsetFavorites
{
    [JsonPropertyName("beatmapset_ids")]
    public int[] BeatmapsetIds { get; set; } = [];
}