using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api;

public class BeatmapsetFavorites
{
    [JsonPropertyName("beatmapset_ids")]
    public int[] BeatmapsetIds { get; set; } = [];
}