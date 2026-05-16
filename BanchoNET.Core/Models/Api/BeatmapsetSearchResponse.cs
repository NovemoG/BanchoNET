using BanchoNET.Core.Models.Api.Beatmaps;

namespace BanchoNET.Core.Models.Api;

public class BeatmapsetSearchResponse
{
    public List<ApiBeatmapsetFull> Beatmapsets { get; set; } = [];
    public BeatmapsetSearch Search { get; set; } = new();
    public string RecommendedDifficulty { get; set; } = "0.0";
    public object? Error { get; set; } = null;
    public long Total { get; set; }
    public Dictionary<string, string> Cursor { get; set; } = new();
    public string CursorString { get; set; } = string.Empty;
}

public class BeatmapsetSearch
{
    public string Sort { get; set; } = "relevance_desc";
}