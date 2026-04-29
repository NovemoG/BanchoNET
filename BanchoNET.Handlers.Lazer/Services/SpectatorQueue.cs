using System.Threading.Channels;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Utils.Replays;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed partial class ScoreSubmissionQueue
{
    private readonly Channel<ClientSpectatorState> _channel = Channel.CreateUnbounded<ClientSpectatorState>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });
    
    private const int TIMEOUT_INTERVAL_SECONDS = 15;
    
    public async Task EnqueueSpectatorScore(
        ClientSpectatorState spectatorState,
        CancellationToken ct = default
    ) {
        await _channel.Writer.WriteAsync(spectatorState, ct);
    }

    protected override async Task WorkerLoop(
        CancellationToken stoppingToken
    ) {
        while (!stoppingToken.IsCancellationRequested)
        {
            ClientSpectatorState spectatorState;
            
            try
            {
                spectatorState = await _channel.Reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await TryProcessScore(spectatorState, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError("Error processing spectator score", ex, nameof(ScoreSubmissionQueue));
            }
        }
    }

    private async Task TryProcessScore(
        ClientSpectatorState spectatorState,
        CancellationToken stoppingToken
    ) {
        var userId = spectatorState.Score!.User!.Id;
        var scoreToken = spectatorState.ScoreToken!.Value;
        
        if (!Scores.TryGetValue(userId, out var scoreRequest))
            return;

        var scope = scopeFactory.CreateScope();
        var scores = scope.ServiceProvider.GetRequiredService<ILazerScoresRepository>();

        if (scoreRequest.Id != scoreToken)
        {
            logger.LogWarning("Score token mismatch");
            Scores.TryRemove(userId, out _);
            
            if (scoreRequest.Score != null)
                await scores.RemoveScore(scoreRequest.Score.Id);
            
            return;
        }

        if (scoreRequest.Score == null)
        {
            if (spectatorState.SubmitTime > DateTime.UtcNow.AddSeconds(-TIMEOUT_INTERVAL_SECONDS))
            {
                logger.LogWarning("Score submission timed out");
                Scores.TryRemove(userId, out _);
                return;
            }
            
            logger.LogDebug($"Score with token ({scoreRequest.Id}) not submitted yet, retrying...");
            
            await Task.Delay(300, stoppingToken);
            await EnqueueSpectatorScore(spectatorState, stoppingToken);
            return;
        }

        var scoreId = scoreRequest.Score.Id;
        if (!scoreRequest.Score.Passed)
            return;

        scoreRequest.Score.User = spectatorState.Score.User;
        
        //TODO validate
        
        try
        {
            logger.LogInfo($"Writing replay for score {scoreId}");
            
            ReplaySerializer.Serialize(scoreRequest.Score, spectatorState.Frames, scoreRequest.Beatmap!);
            await scores.ToggleScoreReplayAvailability(scoreId);
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
            Scores.TryRemove(userId, out _);
        }
    }
}