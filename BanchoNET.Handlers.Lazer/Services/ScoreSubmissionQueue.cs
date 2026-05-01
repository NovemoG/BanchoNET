using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Handlers.Lazer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed partial class ScoreSubmissionQueue(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache,
    IHubContext<SpectatorHub, ISpectatorClient> spectatorHub
) : ConcurrentBackgroundQueue, IScoreSubmissionQueue
{
    private static long _nextScoreId;
    private static long GetNextScoreId => Interlocked.Increment(ref _nextScoreId);
    
    private static readonly MemoryCacheEntryOptions ScoreCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    private bool TryGetScore(long id, out ScoreResponseDto? score)
        => cache.TryGetValue(id, out score);
    
    public async Task<ScoreResponseDto?> EnqueueScore(
        ScoreRequestDto request,
        int userId,
        int beatmapId
    ) {
        //TODO store scores in db and retrieve them after restart

        using var scope = scopeFactory.CreateScope();
        var beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapsRepository>();

        var beatmap = await beatmaps.GetBeatmap(request.beatmap_hash);
        if (beatmap == null)
            return null;
        
        var response = new ScoreResponseDto
        {
            BeatmapId = beatmapId,
            CreatedAt = DateTimeOffset.UtcNow,
            Id = GetNextScoreId,
            UserId = userId
        };

        cache.Set(response.Id, response, ScoreCacheOptions);
        return response;
    }

    public async Task<ApiScore?> SubmitScore(
        ScoreSubmitRequestDto request,
        long queueId,
        int userId,
        int beatmapId
    ) {
        if (!TryGetScore(queueId, out var soloRequest)
            || soloRequest!.UserId != userId
            || soloRequest.BeatmapId != beatmapId)
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
            Accuracy = request.Accuracy * 100,
            MaxCombo = request.MaxCombo,
            Rank = request.Rank,
            Grade = Enum.Parse<Grade>(request.Rank, true),
            Mods = request.Mods ?? [],
            LegacyMods = request.Mods?.ToLegacyMods() ?? 0,
            Statistics = request.Statistics,
            MaximumStatistics = request.MaximumStatistics,
            
            ClassicTotalScore = 0, //TODO calculate
            Preserve = false,
            Processed = true,
            Ranked = beatmap.Status is BeatmapStatus.Ranked or BeatmapStatus.Approved,
            BeatmapId = beatmapId,
            BestId = null, //TODO
            UserId = userId,
            BuildId = 0, //TODO
            EndedAt = DateTimeOffset.UtcNow,
            HasReplay = false,
            IsPerfectCombo = beatmap.MaxCombo == request.MaxCombo,
            LegacyPerfect = beatmap.MaxCombo == request.MaxCombo, //TODO these can differ?
            LegacyScoreId = null, //TODO
            LegacyTotalScore = null, //TODO calculate
            StartedAt = soloRequest.CreatedAt,
            Replay = false,
            Pauses = request.Pauses,
        };
        //TODO check what exactly is stored in pauses
        apiScore.TimeElapsed = (int)((apiScore.EndedAt - apiScore.StartedAt!).Value.TotalSeconds - request.Pauses.Sum() / 10000d);
        SetClockRate(apiScore);
        
        var mode = (GameMode)apiScore.RulesetId;
        
        var prevBest = await scores.GetPlayerBestScoreOnMap(userId, mode, beatmap);
        var sameMods = apiScore.EqualModsWith(prevBest);
        
        var bestWithMods = prevBest != null && !sameMods
            ? await scores.GetPlayerBestScoreWithModsOnMap(userId, mode, apiScore.Mods, beatmap)
            : null;
        
        if (await beatmapHandler.EnsureLocalBeatmapFile(beatmap.Id, beatmap.MD5))
        {
            if (apiScore.Passed)
            {
                apiScore.CalculatePerformance(beatmap);
                
                ComputeSubmissionStatus(apiScore, prevBest, bestWithMods, sameMods);

                apiScore.Preserve = apiScore.Status > SubmissionStatus.Submitted;
                
                await scores.UpdateScoreStatus(prevBest);
                await scores.UpdateScoreStatus(bestWithMods);
                
                if (beatmap.Status != BeatmapStatus.LatestPending)
                    await scores.SetScoreLeaderboardPosition(apiScore, withMods: false, beatmap);
            }
            else apiScore.Status = SubmissionStatus.Failed;
        }
        else
        {
            apiScore.Pp = 0;
            apiScore.Status = apiScore.Passed ? SubmissionStatus.Submitted : SubmissionStatus.Failed;
        }
        
        soloRequest.Score = await scores.InsertScore(apiScore, false, beatmap.MD5, beatmapId);
        soloRequest.Beatmap = beatmap;
        
        var stats = (await players.GetPlayerModeStats(userId, (byte)mode))!;

        await RecalculatePlayerStats(players, beatmap, player, stats, mode, apiScore, prevBest, bestWithMods);
        await players.UpdatePlayerStats(stats, apiScore);
        await players.UpdateLatestActivity(userId);
        
        if (!player.IsRestricted)
        {
            beatmap.Plays += 1;
            if (apiScore.Passed)
                beatmap.Passes += 1;
			
            await beatmaps.UpdateBeatmapPlayCount(beatmap);
        }

        return apiScore;
    }

    private static void SetClockRate(
        ApiScore score
    ) {
        var dt = score.Mods.FirstOrDefault(m => m.Acronym is "DT" or "NC" or "HT" or "DC");
        
        score.ClockRate = dt != null ? 1.5d : 1d;
        if (dt != null && dt.Settings.TryGetValue("speed_change", out var rateChange))
            score.ClockRate = rateChange.GetDouble();
    }
    
    private static void ComputeSubmissionStatus(
        ApiScore newScore,
        ApiScore? prevBest,
        ApiScore? bestWithMods,
        bool sameMods
    ) {
        // if we beat prevBest
        if (newScore.IsBetterThan(prevBest))
        {
            newScore.Status = SubmissionStatus.Best;
            
            // if prevBest exists, we update its status depending on if mods are equal
            if (prevBest != null)
            {
                prevBest.Status = !sameMods
                    ? SubmissionStatus.BestWithMods
                    : SubmissionStatus.Submitted;

                newScore.PreviousBest = prevBest;
            }
        }
        else
        {
            // prevBest must exist because the current score is worse
            newScore.Status = !sameMods
                ? SubmissionStatus.BestWithMods
                : SubmissionStatus.Submitted;

            newScore.PreviousBest = prevBest;
        }

        // the new score is not the best, but the bestWithMods does not exist, so we return early
        // we also compare if prevBest is the same as bestWithMods and return if yes (no need to compare further)
        if (bestWithMods == null || prevBest?.Id == bestWithMods.Id)
            return;
        
        // the new score is not the best
        if (newScore.Status != SubmissionStatus.Best)
        {
            // but is better than bestWithMods
            if (newScore.IsBetterThan(bestWithMods))
            {
                newScore.Status = SubmissionStatus.BestWithMods;
                bestWithMods.Status = SubmissionStatus.Submitted;
            }
            // but is not better than bestWithMods, it is not any leaderboard worthy score
            else
            {
                newScore.Status = SubmissionStatus.Submitted;
            }
        }
        else
        {
            // the new score is better than bestWithMods
            bestWithMods.Status = SubmissionStatus.Submitted;
        }
    }
    
    private static async Task RecalculatePlayerStats(
        IPlayersRepository players,
        Beatmap beatmap,
        Player player,
        StatsDto stats,
        GameMode mode,
        ApiScore score,
        ApiScore? prevBest,
        ApiScore? bestWithMods
    ) {
        if (!score.Passed || !beatmap.AwardsPP())
            return;
        
        if (score.MaxCombo > stats.MaxCombo)
            stats.MaxCombo = score.MaxCombo;
        
        if (score.Status == SubmissionStatus.Best)
        {
            var oldBestScore = 0;
            
            if (prevBest != null)
            {
                // our current score is best, so if prevbest is submitted subtract,
                // otherwise our score should still count because it is in the modded
                // leaderboard; then if current score beat both prevBest and bestWithMods
                // but prevBest is BestWithMods we subtract bestWithMods
                if (prevBest is { Status: SubmissionStatus.Submitted, Grade: >= Grade.A })
                    DecreaseGrade(stats, prevBest.Grade);
                else if (bestWithMods != null)
                    DecreaseGrade(stats, bestWithMods.Grade);
                
                oldBestScore = prevBest.TotalScore;
            }
            
            stats.RankedScore += score.TotalScore - oldBestScore;
            
            if (score.Grade >= Grade.A)
                IncreaseGrade(stats, score.Grade);
            
            await players.RecalculatePlayerTopScores(player.Id, stats, mode);
            await players.UpdatePlayerRank(player.Id, player.IsRestricted, player.CountryCode.ToString(), stats, mode);
        }
        else if (score.Status == SubmissionStatus.BestWithMods)
        {
            // if our score didnt beat prevBest but beat bestWithMods subtract
            if (bestWithMods is { Grade: >= Grade.A })
                DecreaseGrade(stats, bestWithMods.Grade);
            
            if (score.Grade >= Grade.A)
                IncreaseGrade(stats, score.Grade);
            
            //TODO save db
        }
    }

    private static void IncreaseGrade(
        StatsDto stats,
        Grade grade
    ) {
        switch (grade)
        {
            case Grade.A: stats.ACount++; break;
            case Grade.S: stats.SCount++; break;
            case Grade.SH: stats.SHCount++; break;
            case Grade.X: stats.XCount++; break;
            case Grade.XH: stats.XHCount++; break;
            default: return;
        }
    }

    private static void DecreaseGrade(
        StatsDto stats,
        Grade grade
    ) {
        switch (grade)
        {
            case Grade.A: stats.ACount--; break;
            case Grade.S: stats.SCount--; break;
            case Grade.SH: stats.SHCount--; break;
            case Grade.X: stats.XCount--; break;
            case Grade.XH: stats.XHCount--; break;
            default: return;
        }
    }
}