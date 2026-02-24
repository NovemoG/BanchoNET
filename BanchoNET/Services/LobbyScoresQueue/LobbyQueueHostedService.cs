using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Services.Repositories;

namespace BanchoNET.Services.LobbyScoresQueue;

public class LobbyQueueHostedService(
    IServiceScopeFactory scopeFactory,
    ILobbyScoresQueue lobbyQueue
) : BackgroundService
{
    private const int MAX_CONCURRENT_JOBS = 10;
    private const int MAX_RETRIES = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var semaphore = new SemaphoreSlim(MAX_CONCURRENT_JOBS);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await lobbyQueue.ReadJobAsync(stoppingToken);
                
                _ = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(stoppingToken);
                    
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        await ExecuteScoresFetch(request, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ExecuteScoresFetch(ScoreRequestDto request, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var scores = scope.ServiceProvider.GetRequiredService<IScoresRepository>();
        var histories = scope.ServiceProvider.GetRequiredService<IHistoriesRepository>();
        
        var lobby = request.Match;
        var slots = request.Slots;
        List<ScoreDto> submittedScores;
        
        byte i = 0;
        do
        {
            submittedScores = await scores.GetMultiplayerScores(
                slots,
                request.MapFinishDate);
            
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        } while (submittedScores.Count != slots.Count && i++ < MAX_RETRIES);

        var scoreEntries = submittedScores.Select(score => new ScoreEntry
            {
                Accuracy = score.Acc,
                Grade = score.Grade,
                Gekis = score.Gekis,
                Count300 = score.Count300,
                Katus = score.Katus,
                Count100 = score.Count100,
                Count50 = score.Count50,
                Misses = score.Misses,
                MaxCombo = score.MaxCombo,
                Mods = score.Mods,
                PlayerId = score.PlayerId,
                TotalScore = score.Score,
                Failed = score.Status == 0,
                Team = (byte)lobby.GetPlayerSlot(score.PlayerId)!.Team
            })
            .ToList();

        await histories.MapCompleted(lobby.LobbyId, scoreEntries);
    }
}