using System.Net;
using BanchoNET.BeatmapServer.Services;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Utils;
using StackExchange.Redis;

namespace BanchoNET.Services;

public sealed class BeatmapsetCrawler(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis
) : BackgroundService
{
    private const string CursorKey = "bancho:beatmapset-crawler:cursor";
    private const int MaxAttempts = 3;
    private const int ProgressLogInterval = 1000;

    private static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan FailedRunDelay = TimeSpan.FromMinutes(10);

    private readonly IDatabase _redis = redis.GetDatabase();
    private readonly SlidingWindowThrottler _throttler = new(
        1,
        TimeSpan.FromMinutes(1) / AppSettings.BeatmapsetCrawlerRequestsPerMinute
    );

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        if (!AppSettings.BeatmapsetCrawlerEnabled) return;

        logger.LogInfo("Starting beatmapset crawler...", caller: nameof(BeatmapsetCrawler));

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay;

            try
            {
                await Crawl(stoppingToken);
                delay = RunInterval;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError("Beatmapset crawl stopped, retrying later", ex, caller: nameof(BeatmapsetCrawler));
                delay = FailedRunDelay;
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task Crawl(
        CancellationToken cancellationToken
    ) {
        var newest = await GetNewestBeatmapsetId(cancellationToken)
                     ?? throw new InvalidOperationException("Unable to determine the newest official beatmapset");

        var stored = await _redis.StringGetAsync(CursorKey);
        var cursor = stored.HasValue ? (int)stored : 0;

        if (cursor >= newest)
        {
            logger.LogInfo($"Beatmapsets are up to date at {cursor}", caller: nameof(BeatmapsetCrawler));
            return;
        }

        logger.LogInfo($"Crawling beatmapsets {cursor + 1} to {newest}", caller: nameof(BeatmapsetCrawler));

        int imported = 0, unavailable = 0, skipped = 0;

        while (cursor < newest)
        {
            var beatmapsetId = cursor + 1;

            switch (await Import(beatmapsetId, cancellationToken))
            {
                case BeatmapsetImportResult.Imported:
                    imported++;
                    break;

                case BeatmapsetImportResult.Unavailable:
                    unavailable++;
                    break;

                default:
                    skipped++;
                    break;
            }

            cursor = beatmapsetId;
            await _redis.StringSetAsync(CursorKey, cursor);

            if (cursor % ProgressLogInterval == 0)
                logger.LogInfo($"Crawled beatmapsets up to {cursor} of {newest}", caller: nameof(BeatmapsetCrawler));
        }

        logger.LogInfo(
            $"Beatmapset crawl finished at {cursor}: {imported} imported, {unavailable} unavailable, {skipped} skipped",
            caller: nameof(BeatmapsetCrawler)
        );
    }

    private async Task<int?> GetNewestBeatmapsetId(
        CancellationToken cancellationToken
    ) {
        await _throttler.WaitAsync(cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IBeatmapHandler>();

        return await handler.GetNewestBeatmapsetIdFromApi(cancellationToken);
    }
    
    private async Task<BeatmapsetImportResult> Import(
        int beatmapsetId,
        CancellationToken cancellationToken
    ) {
        var failures = 0;

        while (true)
        {
            await _throttler.WaitAsync(cancellationToken);

            BeatmapsetImportResult result;

            try
            {
                // a fresh scope per set keeps the db context from tracking the whole catalogue
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IBeatmapHandler>();

                result = await handler.ImportBeatmapsetFromApi(beatmapsetId, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException
                                       || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                result = BeatmapsetImportResult.Failed;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (++failures >= MaxAttempts)
                {
                    logger.LogError($"Skipping beatmapset {beatmapsetId} after {failures} failed imports", ex, caller: nameof(BeatmapsetCrawler));
                    return BeatmapsetImportResult.Invalid;
                }

                logger.LogWarning($"Import of beatmapset {beatmapsetId} failed, retrying", ex, caller: nameof(BeatmapsetCrawler));
                await Task.Delay(RetryDelay, cancellationToken);
                continue;
            }

            switch (result)
            {
                case BeatmapsetImportResult.RateLimited:
                    logger.LogWarning($"Rate limited by the official server at beatmapset {beatmapsetId}, waiting", caller: nameof(BeatmapsetCrawler));
                    await Task.Delay(RetryDelay, cancellationToken);
                    continue;

                case BeatmapsetImportResult.Failed:
                    if (++failures >= MaxAttempts)
                        throw new HttpRequestException($"Official server unavailable while importing beatmapset {beatmapsetId}", null, HttpStatusCode.ServiceUnavailable);

                    await Task.Delay(RetryDelay, cancellationToken);
                    continue;

                case BeatmapsetImportResult.Invalid:
                    logger.LogWarning($"Skipping beatmapset {beatmapsetId}, the official response couldn't be read", caller: nameof(BeatmapsetCrawler));
                    return result;

                default:
                    return result;
            }
        }
    }
}