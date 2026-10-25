using System.Threading.Channels;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Replays;
using BanchoNET.Core.Utils.Scores;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed partial class ScoreSubmissionQueue
{
    private readonly Channel<(long ScoreToken, int RetryCount)> _channel = Channel.CreateUnbounded<(long ScoreToken, int RetryCount)>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });

    private const int TIMEOUT_INTERVAL_SECONDS = 30;
    private const int MAX_EXCEPTION_RETRIES = 3;

    public async Task EnqueueSpectatorScore(
        long scoreToken,
        CancellationToken ct = default
    ) {
        await _channel.Writer.WriteAsync((scoreToken, 0), ct);
    }
    
    public async Task RequeuePending(
        CancellationToken ct = default
    ) {
        var pending = await states.GetPending();
        if (pending.Length == 0) return;

        logger.LogInfo($"Re-queueing {pending.Length} spectator scores left over from a previous run", nameof(ScoreSubmissionQueue));

        foreach (var scoreToken in pending)
            await EnqueueSpectatorScore(scoreToken, ct);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        try
        {
            await RequeuePending(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to re-queue pending spectator scores", ex, nameof(ScoreSubmissionQueue));
        }

        await base.ExecuteAsync(stoppingToken);
    }

    protected override async Task WorkerLoop(
        CancellationToken stoppingToken
    ) {
        while (!stoppingToken.IsCancellationRequested)
        {
            (long ScoreToken, int RetryCount) pending;
            
            try
            {
                pending = await _channel.Reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await TryProcessScore(pending.ScoreToken, pending.RetryCount, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (pending.RetryCount >= MAX_EXCEPTION_RETRIES)
                {
                    logger.LogError($"Error processing spectator score {pending.ScoreToken} after {MAX_EXCEPTION_RETRIES} retries; leaving pending for recovery", ex, nameof(ScoreSubmissionQueue));
                    continue;
                }

                logger.LogError($"Error processing spectator score {pending.ScoreToken}; retrying ({pending.RetryCount + 1}/{MAX_EXCEPTION_RETRIES})", ex, nameof(ScoreSubmissionQueue));
                await Task.Delay(1000, stoppingToken);
                await _channel.Writer.WriteAsync((pending.ScoreToken, pending.RetryCount + 1), stoppingToken);
            }
        }
    }

    private async Task TryProcessScore(
        long scoreToken,
        int retryCount,
        CancellationToken stoppingToken
    ) {
        var spectatorState = await states.Get(scoreToken);
        if (spectatorState?.Score?.User?.Id is not { } userId)
        {
            await states.ClearPending(scoreToken);
            return;
        }

        var pendingScore = await tokens.Get(scoreToken);
        if (pendingScore == null)
        {
            await states.ClearPending(scoreToken);
            return;
        }

        if (pendingScore.Score == null)
        {
            if (spectatorState.SubmitTime < DateTime.UtcNow.AddSeconds(-TIMEOUT_INTERVAL_SECONDS))
            {
                logger.LogWarning("Score submission timed out");

                await tokens.Remove(scoreToken);
                await states.Remove(scoreToken);
                await states.ClearPending(scoreToken);
                return;
            }

            logger.LogDebug($"Score with token ({scoreToken}) not submitted yet, retrying...");

            await Task.Delay(1000, stoppingToken);
            await _channel.Writer.WriteAsync((scoreToken, retryCount), stoppingToken);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapHandler>();

        pendingScore.RestoreInternalState();

        var score = pendingScore.Score;
        var beatmap = await beatmaps.GetBeatmap(pendingScore.Response.BeatmapId);
        if (beatmap == null)
        {
            logger.LogWarning($"Beatmap {pendingScore.Response.BeatmapId} vanished before score {score.Id} could be processed");

            await Cleanup(scoreToken);
            return;
        }

        score.User = spectatorState.Score.User;

        var frames = await states.GetFrames(scoreToken);
        
        var service = scope.ServiceProvider.GetRequiredService<IScoreService>();
        int? seconds = frames.Count == 0
            ? null
            : ScoreProcessing.LazerPlayTimeSeconds(
                frames[^1].Time,
                beatmap.TotalLength - beatmap.HitLength,
                score.ClockRate
            );
        
        if (seconds != null) score.TimeElapsed = seconds.Value;
        
        Func<string, Task>? writeReplay = null;
        if (score.Passed && frames.Count > 0)
        {
            writeReplay = path =>
            {
                ReplaySerializer.Serialize(score, frames, beatmap, path);
                return Task.CompletedTask;
            };
        }

        var completion = await service.Complete(score.Id, seconds, writeReplay);
        if (completion.FirstCompletion)
            await UpdateBeatmapStats(scope, score, beatmap, userId);
        
        if (score.Passed && completion.HasReplay)
            await spectatorHub.Clients.User(userId.ToString()).UserScoreProcessed(userId, score.Id);
        else if (score.Passed)
            logger.LogWarning($"No replay frames stored for score {score.Id}, skipping replay");

        await Cleanup(scoreToken);
    }

    private async Task Cleanup(
        long scoreToken
    ) {
        await tokens.Remove(scoreToken);
        await states.Remove(scoreToken);
        await states.ClearPending(scoreToken);
    }
}