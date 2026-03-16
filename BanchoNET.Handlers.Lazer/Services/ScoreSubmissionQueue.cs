using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Lazer.Services;

public class ScoreSubmissionQueue(
    IServiceScopeFactory scopeFactory
) : IScoreSubmissionQueue
{
    private readonly ConcurrentDictionary<int, ScoreResponseDto> _scores = new();
    private long GetNextScoreId => Interlocked.Increment(ref field);
    
    public async Task<ScoreResponseDto?> EnqueueScore(
        ScoreRequestDto request,
        int userId,
        int beatmapId
    ) {
        var createdAt = DateTimeOffset.UtcNow;
        //TODO there might be a case where player starts a map and then loses connection and never submits
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
        var players = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();
        
        var player = await players.GetPlayerOrOffline(userId);
        if (player == null) return null;
        
        var beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapsRepository>();
        
        var beatmap = await beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null) return null;
        
        var beatmapHandler = scope.ServiceProvider.GetRequiredService<IBeatmapHandler>();
        var scores = scope.ServiceProvider.GetRequiredService<ILazerScoresRepository>();

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
            BestId = null, //TODO
            UserId = userId,
            BuildId = 0, //TODO
            EndedAt = DateTimeOffset.UtcNow,
            HasReplay = false, //TODO figure out how the replay is sent to server??
            IsPerfectCombo = beatmap.MaxCombo == request.MaxCombo,
            LegacyPerfect = beatmap.MaxCombo == request.MaxCombo, //TODO these can differ?
            LegacyScoreId = null, //TODO
            LegacyTotalScore = null, //TODO calculate
            StartedAt = value.CreatedAt,
            Replay = false,
        };

        //TODO lazer score submission
        var mode = (GameMode)apiScore.RulesetId;
        
        if (await beatmapHandler.EnsureLocalBeatmapFile(beatmap.Id, beatmap.MD5))
        {
            apiScore.CalculatePerformance(beatmap);
        }
        else
        {
            apiScore.Pp = 0;
            apiScore.Status = apiScore.Passed ? SubmissionStatus.Submitted : SubmissionStatus.Failed;
        }

        /*var timeElapsed = (int)(apiScore.EndedAt - apiScore.StartedAt).Value.TotalSeconds;
        var stats = player.Stats[mode];

        stats.IncreasePlaytime(apiScore.LegacyMods, timeElapsed);
        stats.PlayCount += 1; 
        stats.TotalScore += apiScore.TotalScore;
        stats.UpdateHits(apiScore);
        
        await players.UpdatePlayerStats(player, mode);*/
        
        if (!player.IsRestricted)
        {
            beatmap.Plays += 1;
            if (apiScore.Passed)
                beatmap.Passes += 1;
			
            await beatmaps.UpdateBeatmapPlayCount(beatmap);
        }
        
        return await scores.InsertScore(apiScore, false, beatmap.MD5, beatmapId);
    }
}