using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.BeatmapServer.Services;

public sealed class BeatmapDownloader(
    ILogger logger,
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory,
    IMemoryCache memoryCache
) : IBeatmapDownloader
{
    private readonly Func<int, string> _getBeatmapsetPath = Storage.GetBeatmapsetPath;
    private readonly SlidingWindowThrottler _throttler = new(AppSettings.BeatmapDownloadsPerHour, TimeSpan.FromHours(1));
    private readonly ConcurrentDictionary<int, Lazy<Task<bool>>> _inFlightDownloads = new();

    internal BeatmapDownloader(
        ILogger logger,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IMemoryCache memoryCache,
        Func<int, string> getBeatmapsetPath
    ) : this(logger, httpClientFactory, scopeFactory, memoryCache)
    {
        _getBeatmapsetPath = getBeatmapsetPath;
    }

    public async Task<bool> EnsureBeatmapset(
        int beatmapsetId,
        CancellationToken cancellationToken = default
    ) {
        if (beatmapsetId < 1)
            return false;

        cancellationToken.ThrowIfCancellationRequested();

        var task = _inFlightDownloads.GetOrAdd(
            beatmapsetId,
            id => new Lazy<Task<bool>>(
                () => EnsureBeatmapsetCore(id),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );

        return await task.Value.WaitAsync(cancellationToken);
    }

    private async Task<bool> EnsureBeatmapsetCore(
        int beatmapsetId
    ) {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var cancellationToken = timeout.Token;
            
            await using var scope = scopeFactory.CreateAsyncScope();
            var beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapsRepository>();
            
            var set = await beatmaps.GetBeatmapset(beatmapsetId);
            var path = _getBeatmapsetPath(beatmapsetId);

            if (set?.IsPrivateUpload == true)
                return File.Exists(path);

            var metadataKey = (nameof(BeatmapDownloader), "metadata", beatmapsetId);
            if (!memoryCache.TryGetValue(metadataKey, out _))
            {
                memoryCache.Set(metadataKey, true, TimeSpan.FromSeconds(30));
                try
                {
                    // this must use a separate beatmapset check cooldown, because otherwise we could
                    // have a situation where osz has newer beatmaps than are stored in db
                    var handler = scope.ServiceProvider.GetRequiredService<IBeatmapHandler>();
                    if (await handler.GetBeatmapset(beatmapsetId, recheckApi: true, cancellationToken: cancellationToken) != null)
                        memoryCache.Set(metadataKey, true, TimeSpan.FromMinutes(5));
                }
                catch (HttpRequestException ex)
                {
                    logger.LogWarning($"Metadata refresh failed for beatmapset {beatmapsetId}", ex);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning($"Metadata refresh timed out for beatmapset {beatmapsetId}", ex);
                }

                set = await beatmaps.GetBeatmapset(beatmapsetId);
            }

            if (set == null)
                return false;

            var checksums = set.Beatmaps.ToDictionary(beatmap => beatmap.Id, beatmap => beatmap.MD5);
            if (checksums.Count == 0 || checksums.Values.Any(string.IsNullOrWhiteSpace))
                return false;

            if (await BeatmapArchive.IsCurrent(path, checksums, cancellationToken))
                return true;

            var retryKey = (nameof(BeatmapDownloader), "retry", beatmapsetId);
            if (memoryCache.TryGetValue(retryKey, out _))
                return false;

            var downloaded = false;
            if (_throttler.TryRegister())
            {
                var url = $"https://osu.ppy.sh/beatmapsets/{beatmapsetId}/download?noVideo=1";
                downloaded = await DownloadBeatmapset(beatmapsetId, url, checksums, beatmaps, cancellationToken);
            }

            if (!downloaded)
            {
                var url = $"{AppSettings.OsuDirectDownloadEndpoint}/{beatmapsetId}?n=1";
                downloaded = await DownloadBeatmapset(beatmapsetId, url, checksums, beatmaps, cancellationToken);
            }

            if (!downloaded)
            {
                memoryCache.Set(retryKey, true, TimeSpan.FromSeconds(30));
                return false;
            }

            memoryCache.Remove(retryKey);
            return true;
        }
        catch (Exception ex)
        {
            memoryCache.Set((nameof(BeatmapDownloader), "retry", beatmapsetId), true, TimeSpan.FromSeconds(30));
            logger.LogError($"Download failed for beatmapset {beatmapsetId}", ex);
            return false;
        }
        finally
        {
            _inFlightDownloads.TryRemove(beatmapsetId, out _);
        }
    }
    
    private async Task StoreRefreshedCookie() {
        await using var scope = scopeFactory.CreateAsyncScope();

        if (scope.ServiceProvider.GetService<OsuSessionCookieJar>() is { } jar)
            await jar.StoreRefreshedCookie();
    }

    private async Task<bool> DownloadBeatmapset(
        int beatmapsetId,
        string url,
        IReadOnlyDictionary<int, string> checksums,
        IBeatmapsRepository beatmaps,
        CancellationToken cancellationToken
    ) {
        var path = _getBeatmapsetPath(beatmapsetId);
        var tempPath = path + $".{Guid.NewGuid()}.tmp";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Referrer = new Uri($"https://osu.ppy.sh/beatmapsets/{beatmapsetId}");
            
            var client = httpClientFactory.CreateClient(nameof(BeatmapDownloader));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (url.StartsWith("https://osu.ppy.sh/", StringComparison.Ordinal))
                await StoreRefreshedCookie();
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning($"Download failed for beatmapset {beatmapsetId}. Status: {response.StatusCode}");
                return false;
            }

            await using (var file = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             8192, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await response.Content.CopyToAsync(file, cancellationToken);
            }

            if (!await BeatmapArchive.Matches(tempPath, checksums, cancellationToken))
            {
                logger.LogWarning($"Downloaded archive does not match beatmapset {beatmapsetId}");
                return false;
            }

            var currentSet = await beatmaps.GetBeatmapset(beatmapsetId);
            if (currentSet == null)
                return false;

            var currentChecksums = currentSet.Beatmaps.ToDictionary(beatmap => beatmap.Id, beatmap => beatmap.MD5);
            var version = BeatmapArchive.GetVersion(checksums);
            
            if (BeatmapArchive.GetVersion(currentChecksums) != version)
                return false;

            await BeatmapArchive.Publish(tempPath, path, version, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
        {
            logger.LogWarning($"Download failed for beatmapset {beatmapsetId}", ex);
            return false;
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}