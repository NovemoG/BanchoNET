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
) : BackgroundService, IBeatmapsetCrawler
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

    private volatile BeatmapsetCrawlerStatus _status = new("disabled", 0);

    public BeatmapsetCrawlerStatus Status => _status;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        try
        {
            var stored = await _redis.StringGetAsync(CursorKey);
            _status = _status with { Cursor = stored.HasValue ? (int)stored : 0 };

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!AppSettings.BeatmapsetCrawlerEnabled)
                {
                    _status = _status with { State = "disabled", NextRunAt = null };
                    return;
                }

                TimeSpan delay;

                try
                {
                    await Crawl(stoppingToken);
                    delay = RunInterval;
                    _status = _status with { State = "waiting" };
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError("Beatmapset crawl stopped, retrying later", ex, caller: nameof(BeatmapsetCrawler));
                    delay = FailedRunDelay;
                    _status = _status with { State = "retrying", LastError = ex.Message };
                }

                _status = _status with { NextRunAt = DateTimeOffset.UtcNow + delay };
                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task Crawl(
        CancellationToken cancellationToken
    ) {
        _status = _status with { State = "crawling", NextRunAt = null, LastError = null };

        var newest = await GetNewestBeatmapsetId(cancellationToken)
                     ?? throw new InvalidOperationException("Unable to determine the newest official beatmapset");

        var stored = await _redis.StringGetAsync(CursorKey);
        var cursor = stored.HasValue ? (int)stored : 0;

        _status = _status with
        {
            Cursor = cursor,
            Newest = newest,
            Imported = 0,
            Unavailable = 0,
            Skipped = 0,
            RunStartedAt = DateTimeOffset.UtcNow,
            RunFinishedAt = null,
        };

        if (cursor >= newest)
        {
            logger.LogInfo($"Beatmapsets are up to date at {cursor}", caller: nameof(BeatmapsetCrawler));
            _status = _status with { RunFinishedAt = DateTimeOffset.UtcNow };
            return;
        }

        logger.LogInfo($"Crawling beatmapsets {cursor + 1} to {newest}", caller: nameof(BeatmapsetCrawler));

        while (cursor < newest)
        {
            var beatmapsetId = cursor + 1;
            var result = await Import(beatmapsetId, cancellationToken);

            cursor = beatmapsetId;
            await _redis.StringSetAsync(CursorKey, cursor);

            _status = result switch
            {
                BeatmapsetImportResult.Imported => _status with { Cursor = cursor, Imported = _status.Imported + 1 },
                BeatmapsetImportResult.Unavailable => _status with { Cursor = cursor, Unavailable = _status.Unavailable + 1 },
                _ => _status with { Cursor = cursor, Skipped = _status.Skipped + 1 },
            };

            if (cursor % ProgressLogInterval == 0)
                logger.LogInfo($"Crawled beatmapsets up to {cursor} of {newest}", caller: nameof(BeatmapsetCrawler));
        }

        _status = _status with { RunFinishedAt = DateTimeOffset.UtcNow };
        
        logger.LogInfo(
            $"Beatmapset crawl finished at {cursor}: {_status.Imported} imported, {_status.Unavailable} unavailable, {_status.Skipped} skipped",
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