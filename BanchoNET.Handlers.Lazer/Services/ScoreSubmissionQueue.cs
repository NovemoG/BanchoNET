using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Lazer.Services;

public class ScoreSubmissionQueue(
    IServiceScopeFactory scopeFactory
) : IScoreSubmissionQueue
{
    private readonly ConcurrentDictionary<long, ScoreResponseDto> _scores = new();
    private long GetNextScoreId => Interlocked.Increment(ref field); //TODO init with last score id
    
    public async Task<ScoreResponseDto?> EnqueueScore(
        ScoreRequestDto request,
        int userId,
        int beatmapId
    ) {
        var queueId = request.QueueId!.Value;
        if (_scores.TryGetValue(queueId, out var value))
        {
            return userId == value.UserId
                ? value
                : null;
        }

        using var scope = scopeFactory.CreateScope();
        var beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapsRepository>();

        var beatmap = await beatmaps.GetBeatmap(request.beatmap_hash);
        if (beatmap == null || beatmap.Id != beatmapId) return null;
        
        var response = new ScoreResponseDto
        {
            BeatmapId = beatmapId,
            CreatedAt = DateTimeOffset.UtcNow,
            Id = GetNextScoreId,
            UserId = userId
        };

        return _scores.TryAdd(queueId, response)
            ? response
            : null;
    }

    public async Task<ApiScore> SubmitScore(
        long queueId,
        ScoreSubmitRequestDto request
    ) {
        throw new NotImplementedException();
    }
}