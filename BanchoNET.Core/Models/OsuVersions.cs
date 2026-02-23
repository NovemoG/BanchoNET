using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models;

public class ClientBuildVersions
{
    [JsonPropertyName("builds")]
    public required Builds[] Builds { get; set; }
}

public class Builds
{
    [JsonPropertyName("changelog_entries")]
    public required ChangelogEntries[] ChangelogEntries { get; set; }
    [JsonPropertyName("version")]
    public required string Version { get; set; }
}

public class ChangelogEntries
{
    [JsonPropertyName("major")]
    public bool Major { get; set; }
}