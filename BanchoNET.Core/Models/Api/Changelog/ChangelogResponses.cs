namespace BanchoNET.Core.Models.Api.Changelog;

public class ChangelogIndexResponse
{
    public List<ChangelogBuild> Builds { get; set; } = [];
    public List<ChangelogStream> Streams { get; set; } = [];
}

public class ChangelogBuild
{
    public long Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string DisplayVersion { get; set; } = string.Empty;
    public long Users { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ChangelogStream UpdateStream { get; set; } = null!;
    public List<ChangelogEntry> ChangelogEntries { get; set; } = [];
    public ChangelogVersions? Versions { get; set; }
}

public class ChangelogVersions
{
    public ChangelogBuild? Next { get; set; }
    public ChangelogBuild? Previous { get; set; }
}

public class ChangelogStream
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public int UserCount { get; set; }
    public ChangelogBuild? LatestBuild { get; set; }
}

public class ChangelogEntry
{
    public long Id { get; set; }
    public string? Repository { get; set; }
    public long? GithubPullRequestId { get; set; }
    public string? GithubUrl { get; set; }
    public string? Url { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? MessageHtml { get; set; }
    public bool Major { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}