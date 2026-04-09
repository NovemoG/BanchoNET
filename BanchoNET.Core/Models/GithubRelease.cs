using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models;

public class GithubFeed
{
    [JsonPropertyName("tag_name")]
    public required string TagName { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }
    
    [JsonPropertyName("published_at")]
    public DateTimeOffset PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public List<GithubRelease> Assets { get; set; } = [];
}

public class GithubRelease
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("content_type")]
    public required string ContentType { get; set; }
    
    [JsonPropertyName("browser_download_url")]
    public required string BrowserDownloadUrl { get; set; }
}