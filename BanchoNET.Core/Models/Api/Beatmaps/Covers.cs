using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class Covers
{
    public string Cover { get; set; } = null!;
    [JsonPropertyName("cover@2x")]
    public string Cover2X { get; set; } = null!;
    
    public string Card { get; set; } = null!;
    [JsonPropertyName("card@2x")]
    public string Card2X { get; set; } = null!;
    
    public string List { get; set; } = null!;
    [JsonPropertyName("list@2x")]
    public string List2X { get; set; } = null!;
    
    public string SlimCover { get; set; } = null!;
    [JsonPropertyName("slimcover@2x")]
    public string SlimCover2X { get; set; } = null!;

    [JsonConstructor]
    public Covers() { }

    public Covers(
        int beatmapsetId,
        long coverId
    ) {
        Cover = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/cover.jpg?{coverId}";
        Cover2X = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/cover@2x.jpg?{coverId}";
        Card = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/card.jpg?{coverId}";
        Card2X = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/card@2x.jpg?{coverId}";
        List = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/list.jpg?{coverId}";
        List2X = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/list@2x.jpg?{coverId}";
        SlimCover = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/slimcover.jpg?{coverId}";
        SlimCover2X = $"https://assets.ppy.sh/beatmaps/{beatmapsetId}/covers/slimcover@2x.jpg?{coverId}";
    }
}