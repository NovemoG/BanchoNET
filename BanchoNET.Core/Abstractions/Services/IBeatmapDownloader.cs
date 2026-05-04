namespace BanchoNET.Core.Abstractions.Services;

public interface IBeatmapDownloader
{
    bool AddBeatmapsetForUpdate(
        int beatmapsetId
    );

    Task<bool> DownloadBeatmap(
        int beatmapsetId,
        CancellationToken cancellationToken = default
    );
}