namespace BanchoNET.Core.Abstractions.Services;

public interface IBeatmapDownloader
{
    Task<bool> EnsureBeatmapset(
        int beatmapsetId,
        CancellationToken cancellationToken = default
    );
}