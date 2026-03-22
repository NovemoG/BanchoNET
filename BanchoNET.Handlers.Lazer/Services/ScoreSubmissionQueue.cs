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
    private static readonly ConcurrentDictionary<int, ScoreResponseDto> Scores = new();
    private static long GetNextScoreId => Interlocked.Increment(ref field);
    
    public async Task<ScoreResponseDto?> EnqueueScore(
        ScoreRequestDto request,
        int userId,
        int beatmapId
    ) {
        var createdAt = DateTimeOffset.UtcNow;
        //TODO there might be a case where player starts a map and then loses connection and never submits
        if (Scores.TryGetValue(userId, out var value))
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

        return Scores.TryAdd(userId, response)
            ? response
            : null;
    }

    public async Task<ApiScore?> SubmitScore(
        ScoreSubmitRequestDto request,
        long queueId,
        int userId,
        int beatmapId
    ) {
        if (!Scores.TryGetValue(userId, out var value)
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
            Grade = Enum.Parse<Grade>(request.Rank, true),
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
        
        var prevBest = await scores.GetPlayerBestScoreOnMap(player.Id, mode, beatmap);
        var bestWithMods = prevBest != null && apiScore.Mods != prevBest.Mods
            ? await scores.GetPlayerBestScoreWithModsOnMap(player.Id, mode, apiScore.Mods, beatmap)
            : null;
        
        if (await beatmapHandler.EnsureLocalBeatmapFile(beatmap.Id, beatmap.MD5))
        {
            apiScore.CalculatePerformance(beatmap);
            
            if (apiScore.Passed)
            {
                ComputeSubmissionStatus(apiScore, prevBest, bestWithMods);
                
                if (beatmap.Status != BeatmapStatus.LatestPending)
                    await scores.SetScoreLeaderboardPosition(apiScore, withMods: false, beatmap);
            }
            else apiScore.Status = SubmissionStatus.Failed;
            
            await scores.UpdateScoreStatus(prevBest);
            await scores.UpdateScoreStatus(bestWithMods);
        }
        else
        {
            apiScore.Pp = 0;
            apiScore.Status = apiScore.Passed ? SubmissionStatus.Submitted : SubmissionStatus.Failed;
        }
        
        await players.UpdatePlayerStats(player, apiScore);
        
        if (!player.IsRestricted)
        {
            beatmap.Plays += 1;
            if (apiScore.Passed)
                beatmap.Passes += 1;
			
            await beatmaps.UpdateBeatmapPlayCount(beatmap);
        }
        
        return await scores.InsertScore(apiScore, false, beatmap.MD5, beatmapId);
    }
    
    private static void ComputeSubmissionStatus(
        ApiScore newScore,
        ApiScore? prevBest,
        ApiScore? bestWithMods
    ) {
        newScore.Status = SubmissionStatus.Submitted;
        
        if (newScore.IsBetterThan(prevBest))
        {
            // if new score beats prevBest and has different mods,
            // prevBest becomes bestWithMods
            prevBest?.Status = newScore.Mods != prevBest.Mods
                ? SubmissionStatus.BestWithMods
                : SubmissionStatus.Submitted;

            newScore.Status = SubmissionStatus.Best;
        }
        else
        {
            // if it didn't beat prevBest, check if it bestWithMods exists and set
            // status accordingly (it is going to be properly checked later)
            if (bestWithMods == null)
                newScore.Status = SubmissionStatus.BestWithMods;
        }

        if (bestWithMods == null || !newScore.IsBetterThan(bestWithMods)) return;
        
        // if it exists and new score is better set bestWithMods to Submitted
        bestWithMods.Status = SubmissionStatus.Submitted;
        
        // this check is for if new score is not better than prevBest but is
        // better than bestWithMods
        if (newScore.Status != SubmissionStatus.Best)
            newScore.Status = SubmissionStatus.BestWithMods;
    }
}