using Newtonsoft.Json;

namespace BanchoNET.Models;

public class ClientBuildVersions
{
    [JsonProperty("build")]
    public required Builds[] Builds { get; set; }
}

public class Builds
{
    [JsonProperty("changelog_entries")]
    public required ChangelogEntries[] ChangelogEntries { get; set; }
    [JsonProperty("version")]
    public required string Version { get; set; }
}

public class ChangelogEntries
{
    [JsonProperty("major")]
    public bool Major { get; set; }
}