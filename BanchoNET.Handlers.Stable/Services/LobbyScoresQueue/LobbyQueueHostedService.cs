using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Handlers.Stable.Services.LobbyScoresQueue;

public class LobbyQueueHostedService(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    ILobbyScoresQueue lobbyQueue
) : ConcurrentBackgroundQueue
{
    private const int MAX_RETRIES = 3;

    protected override async Task WorkerLoop(
        CancellationToken stoppingToken
    ) {
        while (!stoppingToken.IsCancellationRequested)
        {
            MatchScoreRequestDto request;
            
            try
            {
                request = await lobbyQueue.ReadJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                await ExecuteScoresFetch(request, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError("Error processing lobby score request", ex, nameof(LobbyQueueHostedService));
            }
        }
    }

    private async Task ExecuteScoresFetch(
        MatchScoreRequestDto request,
        CancellationToken stoppingToken
    ) {
        using var scope = scopeFactory.CreateScope();
        var scores = scope.ServiceProvider.GetRequiredService<ILegacyScoresRepository>();
        var histories = scope.ServiceProvider.GetRequiredService<IHistoriesRepository>();
        
        var lobby = request.Match;
        var slots = request.Slots;
        List<ScoreDto> submittedScores;
        
        byte i = 0;
        do
        {
            submittedScores = await scores.GetMultiplayerScores(
                slots,
                request.MapFinishDate
            );
            
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        } while (submittedScores.Count != slots.Count && i++ < MAX_RETRIES);

        var scoreEntries = submittedScores.Select(score => new ScoreEntry
            {
                Accuracy = score.Acc,
                Grade = (byte)score.Grade,
                Gekis = score.GetCountGeki(),
                Count300 = score.GetCount300(),
                Katus = score.GetCountKatu(),
                Count100 = score.GetCount100(),
                Count50 = score.GetCount50(),
                Misses = score.GetCountMiss(),
                MaxCombo = score.MaxCombo,
                Mods = (int)score.Mods,
                PlayerId = score.PlayerId,
                TotalScore = (int)score.LegacyTotalScore, //TODO
                Failed = score.Status == 0,
                Team = (byte)lobby.GetPlayerSlot(score.PlayerId)!.Team
            })
            .ToList();

        await histories.MapCompleted(lobby.LobbyId, scoreEntries);
    }
}