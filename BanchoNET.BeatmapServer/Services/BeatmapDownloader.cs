using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils;

namespace BanchoNET.BeatmapServer.Services;

public sealed class BeatmapDownloader(
    ILogger logger,
    IHttpClientFactory httpClientFactory
) : IBeatmapDownloader
{
    private readonly HttpClient _cookieClient = httpClientFactory.CreateClient(nameof(BeatmapDownloader));
    
    private static readonly DownloadThrottler Throttler = new();
    private static readonly ConcurrentDictionary<int, Lazy<Task<bool>>> InFlightDownloads = new();
    private static readonly ConcurrentDictionary<int, bool> BeatmapsetUpdates = new();
    
    public bool AddBeatmapsetForUpdate(int beatmapsetId) => BeatmapsetUpdates.TryAdd(beatmapsetId, true);

    public async Task<bool> DownloadBeatmap(
        int beatmapsetId,
        CancellationToken cancellationToken = default
    ) {
        if (beatmapsetId < 1)
            return false;

        if (!BeatmapsetUpdates.ContainsKey(beatmapsetId))
            return false;

        var lazyTask = InFlightDownloads.GetOrAdd(
            beatmapsetId,
            id => new Lazy<Task<bool>>(
                () => DownloadBeatmapCore(id, CancellationToken.None),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );

        try
        {
            return await lazyTask.Value.WaitAsync(cancellationToken);
        }
        finally
        {
            if (lazyTask is { IsValueCreated: true, Value.IsCompleted: true }
                && InFlightDownloads.TryGetValue(beatmapsetId, out var current)
                && ReferenceEquals(current, lazyTask))
            {
                InFlightDownloads.TryRemove(beatmapsetId, out _);
            }
        }
    }

    private async Task<bool> DownloadBeatmapCore(
        int beatmapsetId,
        CancellationToken cancellationToken
    ) {
        try
        {
            if (!Throttler.TryRegisterDownload())
                return await Fallback(beatmapsetId, cancellationToken);

            logger.LogDebug("Downloading beatmapset from official osu website");

            var url = $"https://osu.ppy.sh/beatmapsets/{beatmapsetId}/download?noVideo=1";
            using var response = await _cookieClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning($"Download failed for beatmapset {beatmapsetId}. Status: {response.StatusCode}");
                return false;
            }

            await using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await SaveBeatmapset(beatmapsetId, downloadStream, cancellationToken);

            BeatmapsetUpdates.TryRemove(beatmapsetId, out _);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Download failed for beatmapset {beatmapsetId}", ex);
            return false;
        }
    }

    private async Task<bool> Fallback(
        int beatmapsetId,
        CancellationToken cancellationToken
    ) {
        logger.LogDebug("Downloading beatmapset from osu.direct");
        
        var url = $"{AppSettings.OsuDirectDownloadEndpoint}{beatmapsetId}?n=1";
        using var response = await _cookieClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning($"Fallback beatmapset download failed for {beatmapsetId}. Status: {response.StatusCode}");
            return false;
        }
        
        await using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await SaveBeatmapset(beatmapsetId, downloadStream, cancellationToken);

        BeatmapsetUpdates.TryRemove(beatmapsetId, out _);
        return true;
    }

    private static async Task SaveBeatmapset(
        int beatmapsetId,
        Stream downloadStream,
        CancellationToken cancellationToken = default
    ) {
        var tempPath = Storage.GetTempBeatmapsetPath();
        await using (var file = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 8192,
                         options: FileOptions.Asynchronous | FileOptions.SequentialScan)
        ) {
            await downloadStream.CopyToAsync(file, cancellationToken);
            await file.FlushAsync(cancellationToken);
        }
        
        var finalPath = Storage.GetBeatmapsetPath(beatmapsetId);
        await WaitForFileRelease(finalPath, cancellationToken);
        
        if (File.Exists(finalPath))
        {
            Logger.Shared.LogDebug($"Replacing existing beatmapset ({beatmapsetId}) file.");
            File.Replace(tempPath, finalPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        else
        {
            Logger.Shared.LogDebug($"Saving beatmapset {beatmapsetId} file.");
            File.Move(tempPath, finalPath);
        }
    }

    private static async Task WaitForFileRelease(
        string path,
        CancellationToken cancellationToken
    ) {
        Logger.Shared.LogDebug($"Waiting for file release: \"{path}\"");
        
        while (true)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                await using var _ = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None
                );

                return;
            }
            catch (IOException)
            {
                await Task.Delay(250, cancellationToken);
            }
        }
    }

    private class DownloadThrottler
    {
        private const int DownloadsPerHour = 170;
        private static readonly TimeSpan Window = TimeSpan.FromHours(1);
        
        private readonly Queue<DateTimeOffset> _downloadTimes = new();
        private readonly Lock _sync = new();

        public bool TryRegisterDownload() {
            lock (_sync)
            {
                var now = DateTimeOffset.UtcNow;
                PruneOldEntries(now);
                
                if (_downloadTimes.Count >= DownloadsPerHour)
                    return false;
                
                _downloadTimes.Enqueue(now);
                return true;
            }
        }

        private void PruneOldEntries(
            DateTimeOffset now
        ) {
            while (_downloadTimes.Count > 0
                   && now - _downloadTimes.Peek() >= Window)
            {
                _downloadTimes.Dequeue();
            }
        }
    }
}