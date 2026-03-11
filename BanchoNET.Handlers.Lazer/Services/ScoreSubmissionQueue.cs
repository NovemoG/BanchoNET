using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Lazer.Services;

public class ScoreSubmissionQueue(
    IServiceScopeFactory scopeFactory
) : IScoreSubmissionQueue
{
    private readonly ConcurrentDictionary<int, ScoreResponseDto> _scores = new();
    private long GetNextScoreId => Interlocked.Increment(ref field); //TODO init with last score id
    
    public async Task<ScoreResponseDto?> EnqueueScore(
        ScoreRequestDto request,
        int userId,
        int beatmapId
    ) {
        var createdAt = DateTimeOffset.UtcNow;
        if (_scores.TryGetValue(userId, out var value))
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
            CreatedAt = createdAt,
            Id = GetNextScoreId,
            UserId = userId
        };

        return _scores.TryAdd(userId, response)
            ? response
            : null;
    }

    public async Task<ApiScore?> SubmitScore(
        ScoreSubmitRequestDto request,
        long queueId,
        int userId,
        int beatmapId
    ) {
        if (!_scores.TryGetValue(userId, out var value)
            || value.UserId != userId
            || value.Id != queueId
            || value.BeatmapId != beatmapId)
        {
            return null;
        }
        
        using var scope = scopeFactory.CreateScope();
        var beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapsRepository>();
        var scores = scope.ServiceProvider.GetRequiredService<IScoresRepository>();
        
        var beatmap = await beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null) return null;
        
        //TODO reserve score id

        var apiScore = new ApiScore
        {
            RulesetId = request.RulesetId,
            Passed = request.Passed,
            TotalScore = request.TotalScore,
            TotalScoreWithoutMods = request.TotalScoreWithoutMods,
            Accuracy = request.Accuracy,
            MaxCombo = request.MaxCombo,
            Rank = request.Rank,
            Mods = request.Mods ?? [],
            Statistics = request.Statistics,
            MaximumStatistics = request.MaximumStatistics,
            
            ClassicTotalScore = 0, //TODO calculate
            Preserve = false, //TODO will handle later
            Processed = false,
            Ranked = beatmap.Status is BeatmapStatus.Ranked or BeatmapStatus.Approved,
            BeatmapId = beatmapId,
            BestId = null, //TODO fetch previous best
            Id = 0, //TODO put reserved score id here
            UserId = userId,
            BuildId = 0, //TODO
            EndedAt = DateTimeOffset.UtcNow,
            HasReplay = false, //TODO figure out how the replay is sent to server??
            IsPerfectCombo = beatmap.MaxCombo == request.MaxCombo,
            LegacyPerfect = beatmap.MaxCombo == request.MaxCombo, //TODO these can differ?
            LegacyScoreId = null, //TODO
            LegacyTotalScore = null, //TODO calculate
            Pp = null, //TODO
            StartedAt = value.CreatedAt,
            Replay = false,
        };
        
        //TODO put in db
        
        return apiScore;
    }
}