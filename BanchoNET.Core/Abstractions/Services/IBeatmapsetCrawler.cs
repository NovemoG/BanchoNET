namespace BanchoNET.Core.Abstractions.Services;

public interface IBeatmapsetCrawler
{
    BeatmapsetCrawlerStatus Status { get; }
}

public sealed record BeatmapsetCrawlerStatus(
    string State,
    int Cursor,
    int? Newest = null,
    int Imported = 0,
    int Unavailable = 0,
    int Skipped = 0,
    DateTimeOffset? RunStartedAt = null,
    DateTimeOffset? RunFinishedAt = null,
    DateTimeOffset? NextRunAt = null,
    string? LastError = null
);