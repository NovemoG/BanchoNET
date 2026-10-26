using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;
using BanchoNET.Handlers.Lazer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed partial class ScoreSubmissionQueue(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    ILazerPlayerService lazerPlayers,
    IScoreTokenStore tokens,
    ISpectatorStateStore states,
    IHubContext<SpectatorHub, ISpectatorClient> spectatorHub
) : ConcurrentBackgroundQueue, IScoreSubmissionQueue
{
    public async Task<ScoreResponseDto?> EnqueueScore(
        ScoreRequestDto request,
        int userId,
        int beatmapId
    ) {
        using var scope = scopeFactory.CreateScope();
        var beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapHandler>();

        var beatmap = await beatmaps.GetBeatmap(request.beatmap_hash);
        if (beatmap == null || beatmap.Id != beatmapId)
            return null;

        var service = scope.ServiceProvider.GetRequiredService<IScoreService>();
        var response = new ScoreResponseDto
        {
            BeatmapId = beatmapId,
            CreatedAt = DateTimeOffset.UtcNow,
            Id = await service.ReserveScoreId(),
            UserId = userId
        };

        await tokens.Store(new PendingScore { Response = response });

        return response;
    }

    public async Task<ApiScore?> SubmitScore(
        ScoreSubmitRequestDto request,
        long queueId,
        int userId,
        int beatmapId
    ) {
        var pending = await tokens.Get(queueId);
        if (pending == null
            || pending.Response.UserId != userId
            || pending.Response.BeatmapId != beatmapId)
        {
            return null;
        }

        var soloRequest = pending.Response;

        using var scope = scopeFactory.CreateScope();

        // The session may predate a restart, so rehydrate rather than dropping the score
        var player = (await lazerPlayers.GetPlayer(userId))?.Player;
        if (player == null)
        {
            var playersRepository = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();

            player = await playersRepository.GetFullPlayerInfo(userId);
            if (player == null) return null;

            await lazerPlayers.AddPlayer(player);
        }

        var beatmaps = scope.ServiceProvider.GetRequiredService<IBeatmapHandler>();
        
        var beatmap = await beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null) return null;
        
        var apiScore = new ApiScore
        {
            Id = soloRequest.Id,
            RulesetId = request.RulesetId,
            Passed = request.Passed,
            TotalScore = request.TotalScore,
            TotalScoreWithoutMods = request.TotalScoreWithoutMods,
            Accuracy = request.Accuracy * 100,
            MaxCombo = request.MaxCombo,
            Rank = request.Rank,
            Grade = Enum.Parse<Grade>(request.Rank, true),
            Mods = request.Mods ?? [],
            ModKeys = request.Mods?.ToKeysString() ?? "",
            LegacyMods = request.Mods?.ToLegacyMods() ?? 0,
            Statistics = request.Statistics,
            MaximumStatistics = request.MaximumStatistics,
            
            Pp = 0,
            ClassicTotalScore = 0, //TODO calculate
            Preserve = false,
            Processed = true,
            Ranked = !player.IsRestricted && beatmap.Status is BeatmapStatus.Ranked or BeatmapStatus.Approved,
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
        
        var service = scope.ServiceProvider.GetRequiredService<IScoreService>();
        var result = await service.Submit(ScoreSubmissionMapping.FromLazer(apiScore, beatmap));
        if (result == null) return null;
        
        ScoreSubmissionMapping.Apply(result.Score, apiScore);
        
        apiScore.ClockRate = apiScore.Mods.GetClockRate();
        apiScore.LeaderboardPosition = result.Position;
        
        pending.Score = apiScore;
        pending.CaptureInternalState();
        
        await tokens.Store(pending);

        return apiScore;
    }

    private async Task UpdateBeatmapStats(
        IServiceScope scope,
        ApiScore score,
        Beatmap beatmap,
        int playerId
    ) {
        if (score.Passed) return;
        
        var lazerPlayer = await lazerPlayers.GetPlayer(playerId);
        if (lazerPlayer != null)
        {
            var index = (int)Math.Round(Math.Min(99, Math.Max(0, (float)score.TimeElapsed / beatmap.HitLength * 100)));

            if (lazerPlayer.LastPlayedBeatmapId != null)
            {
                var beatmapsRepository = scope.ServiceProvider.GetRequiredService<IBeatmapsRepository>();
                
                if (lazerPlayer.LastPlayedBeatmapId == beatmap.Id)
                {
                    beatmap.Fails[index] += 1;

                    await beatmapsRepository.UpdateBeatmapFailTimes(beatmap.Id, index, isFail: true);
                }
                else
                {
                    var lastBeatmap = await beatmapsRepository.GetBeatmap(lazerPlayer.LastPlayedBeatmapId.Value);
                    
                    lastBeatmap?.Exits[lazerPlayer.LastPlayedBeatmapExitIndex] += 1;
                    
                    await beatmapsRepository.UpdateBeatmapFailTimes(
                        lazerPlayer.LastPlayedBeatmapId.Value,
                        lazerPlayer.LastPlayedBeatmapExitIndex,
                        isFail: false
                    );
                }
            }
            
            await lazerPlayers.SetLastPlayed(playerId, beatmap.Id, index);
        }
    }

}