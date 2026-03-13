using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api;

public class Activity
{
    public DateTimeOffset CreatedAt { get; set; }
    [JsonPropertyName("createdAt"), JsonInclude]
    private DateTimeOffset CreatedAt2 => CreatedAt;
    
    public long Id { get; set; }
    public string Type { get; set; }
    [JsonPropertyName("scoreRank")]
    public string ScoreRank { get; set; }
    public int Rank { get; set; }
    public string Mode { get; set; }
    public ActivityBeatmap Beatmap { get; set; }
    public ActivityUser User { get; set; }
}

public class ActivityBeatmap
{
    public string Title { get; set; }
    public string Url { get; set; }
}

public class ActivityUser
{
    public string Username { get; set; }
    public string Url { get; set; }
}