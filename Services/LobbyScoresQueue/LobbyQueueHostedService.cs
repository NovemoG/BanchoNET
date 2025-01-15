using BanchoNET.Abstractions.Services;
using BanchoNET.Models.Dtos;
using BanchoNET.Models.Mongo;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils.Extensions;

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
                        
                        using var scope = scopeFactory.CreateScope();
                        var scores = scope.ServiceProvider.GetRequiredService<ScoresRepository>();
                        var histories = scope.ServiceProvider.GetRequiredService<HistoriesRepository>();
                        
                        var lobby = request.Lobby;
                        var slots = request.Slots;
                        List<ScoreDto> submittedScores;
                        
                        byte i = 0;
                        do
                        {
                            submittedScores = await scores.GetPlayersRecentScores(
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
}