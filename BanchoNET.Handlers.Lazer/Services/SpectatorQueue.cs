using System.Threading.Channels;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils.Replays;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed partial class ScoreSubmissionQueue
{
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });

    private const int TIMEOUT_INTERVAL_SECONDS = 10;

    public async Task EnqueueSpectatorScore(
        long scoreToken,
        CancellationToken ct = default
    ) {
        await _channel.Writer.WriteAsync(scoreToken, ct);
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
            long scoreToken;
            
            try
            {
                scoreToken = await _channel.Reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await TryProcessScore(scoreToken, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError("Error processing spectator score", ex, nameof(ScoreSubmissionQueue));
            }
        }
    }

    private async Task TryProcessScore(
        long scoreToken,
        CancellationToken stoppingToken
    ) {
        var spectatorState = await states.Get(scoreToken);
        if (spectatorState?.Score?.User == null)
        {
            await states.ClearPending(scoreToken);
            return;
        }

        var userId = spectatorState.Score.User.Id;

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

            await Task.Delay(250, stoppingToken);
            await EnqueueSpectatorScore(scoreToken, stoppingToken);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var players = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();
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
        
        if (frames.Count > 0)
        {
            var scoreTime = frames[^1].Time / 1000d;
            score.TimeElapsed = (int)Math.Round((scoreTime - (beatmap.TotalLength - beatmap.HitLength)) / score.ClockRate);
        }

        await UpdateBeatmapStats(scope, score, beatmap, userId);
        await players.IncreasePlayerPlayTime(userId, score.RulesetId, score.TimeElapsed);

        if (!score.Passed)
        {
            await Cleanup(scoreToken);
            return;
        }

        var scoreId = score.Id;

        //TODO validate

        try
        {
            if (frames.Count == 0)
            {
                logger.LogWarning($"No replay frames stored for score {scoreId}, skipping replay");
                return;
            }

            var scores = scope.ServiceProvider.GetRequiredService<ILazerScoresRepository>();

            logger.LogInfo($"Writing replay for score {scoreId}");

            ReplaySerializer.Serialize(score, frames, beatmap);
            await scores.ToggleScoreReplayAvailability(scoreId);
            await spectatorHub.Clients.User(userId.ToString()).UserScoreProcessed(userId, scoreId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug($"Score processing cancelled for {scoreId}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error processing score {scoreId}", ex);
        }
        finally
        {
            await Cleanup(scoreToken);
        }
    }

    private async Task Cleanup(
        long scoreToken
    ) {
        await tokens.Remove(scoreToken);
        await states.Remove(scoreToken);
        await states.ClearPending(scoreToken);
    }
}