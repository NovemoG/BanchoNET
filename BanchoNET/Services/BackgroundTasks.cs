using System.Diagnostics;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using Cronos;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BanchoNET.Services;

public class BackgroundTasks(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    IPlayerService playerService
) : BackgroundService, IBackgroundTasks
{
    private readonly Dictionary<string, string> _cronMap = new()
    {
        { "UpdateBotStatus", $"*/{AppSettings.BotStatusUpdateInterval} * * * *" },
        { "CheckSupporters", "*/30 * * * *" },          // every 30 minutes
        { "AppendRankHistory", "0 0 * * *" },           // every day at midnight
        { "MarkInactivePlayers", "0 0 * * *" },         // every day at midnight
        { "DeleteUnnecessaryScores", "0 0 */2 * *" },   // every 2 days at midnight
        { "AppendMonthlyHistory", "0 0 1 * *" },        // every 1st day of the month at midnight
    };

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        logger.LogInfo("Starting background tasks...", caller: nameof(BackgroundTasks));

        try
        {
            UpdateBotStatus();
            await CheckExpiringSupporters(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Error during initial background tasks run.", ex);
        }

        foreach (var (jobName, cronExpression) in _cronMap)
            _ = Task.Run(() => JobLoopAsync(jobName, cronExpression, stoppingToken), stoppingToken);
    }

    private async Task JobLoopAsync(
        string jobName,
        string cronExpression,
        CancellationToken stoppingToken
    ) {
        logger.LogInfo($"Started cron job loop: {jobName} => {cronExpression}");
        
        var cron = CronExpression.Parse(cronExpression);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var next = cron.GetNextOccurrence(now, TimeZoneInfo.Utc);

                if (next == null)
                {
                    logger.LogWarning($"No next occurence for {jobName} (cron: {cronExpression})");
                    return;
                }

                var delay = next.Value - now;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                await ExecuteNamedJob(jobName, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error executing job {jobName}", ex);
            }
        }
        
        logger.LogInfo($"Stopping cron loop for job {jobName}");
    }

    private Task ExecuteNamedJob(
        string jobName,
        CancellationToken ct
    ) {
        switch (jobName)
        {
            case "UpdateBotStatus":
                UpdateBotStatus();
                return Task.CompletedTask;
            
            case "CheckSupporters":
                return CheckExpiringSupporters(ct);

            case "AppendRankHistory":
                return AppendPlayerRankHistory(ct);

            case "MarkInactivePlayers":
                return MarkInactivePlayers(ct);

            case "DeleteUnnecessaryScores":
                return DeleteUnnecessaryScores(ct);

            case "AppendMonthlyHistory":
                return AppendPlayerMonthlyHistory(ct);

            default:
                logger.LogWarning($"Unknown cron job name: {jobName}");
                return Task.CompletedTask;
        }
    }
    
    #region Rank History

    public async Task AppendPlayerRankHistory(CancellationToken ct)
    {
        logger.LogInfo($"Appending players' daily rank history ({DateTime.UtcNow})", caller: nameof(BackgroundTasks));

        for (var i = 0; i < 8; i++)
        {
            await ProcessRankHistory((byte)i, ct); //TODO fix with batch updates
        }

        logger.LogInfo($"Finished updating players' rank history ({DateTime.UtcNow})", caller: nameof(BackgroundTasks));
    }

    private async Task ProcessRankHistory(byte mode, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var histories = scope.ServiceProvider.GetRequiredService<IHistoriesRepository>();
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var key = $"bancho:leaderboard:{mode}";
        var playerCount = await redis.SortedSetLengthAsync(key);
        //var playerCount = await players.TotalPlayerCount();
        const int limit = 10_000;
        
        mode = mode == 7 ? (byte)(mode + 1) : mode;
        
        var iter = 0;
        for (var i = 0; i < playerCount; i += limit)
        {
            var ranks = await redis.SortedSetRangeByRankWithScoresAsync(
                key: key,
                start: i,
                stop: limit + iter * limit - 1,
                order: Order.Descending);
                
            for (var j = 0; j < ranks.Length; j++)
            {
                var playerId = int.Parse(ranks[j].Element!);

                await histories.AddRankHistory(
                    playerId,
                    mode,
                    i + j + 1);
            }
                
            iter++;
        }
        
        stopwatch.Stop();
        logger.LogInfo($"Finished updating daily rank history for mode {mode}, execution time: {stopwatch.Elapsed}",
            caller: nameof(BackgroundTasks));
    }

    #endregion

    #region Monthly History

    public async Task AppendPlayerMonthlyHistory(CancellationToken ct)
    {
        logger.LogInfo($"Appending players' monthly history ({DateTime.UtcNow})", caller: nameof(BackgroundTasks));

        for (var i = 0; i < 8; i++)
        {
            await ProcessMonthlyHistory((byte)i, ct); //TODO fix with batch updates
        }
        
        logger.LogInfo($"Finished updating players' monthly history ({DateTime.UtcNow})", caller: nameof(BackgroundTasks));
    }
    
    private async Task ProcessMonthlyHistory(byte mode, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();
        var histories = scope.ServiceProvider.GetRequiredService<IHistoriesRepository>();
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var playerCount = await players.TotalPlayerCount();
        const int limit = 10_000;
        
        mode = mode == 7 ? (byte)(mode + 1) : mode;
        
        var iter = 0;
        for (int i = 0; i < playerCount; i += limit)
        {
            var stats = await players.GetPlayersModeStatsRange(mode, limit, iter * limit);
            
            foreach (var (playerId, playCount, replaysViewed) in stats)
            {
                await Task.WhenAll(
                    histories.AddPlayCountHistory(
                        playerId,
                        mode,
                        playCount),
                    histories.AddReplaysHistory(
                        playerId,
                        mode,
                        replaysViewed)
                );
            }
                
            iter++;
        }

        await players.ResetPlayersStats(mode);
        
        stopwatch.Stop();
        logger.LogInfo($"Finished updating monthly history for mode {mode}, execution time: {stopwatch.Elapsed}",
            caller: nameof(BackgroundTasks));
    }

    #endregion
    
    public async Task MarkInactivePlayers(CancellationToken ct)
    {
        logger.LogInfo("Marking inactive players...", caller: nameof(BackgroundTasks));
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
        
        var inactivePlayers = await db.Players
            .Where(p => !p.Inactive && p.LastActivityTime < DateTime.UtcNow.AddDays(-AppSettings.DaysUntilPlayerIsMarkedInactive))
            .ExecuteUpdateAsync(p => p.SetProperty(u => u.Inactive, true));
        
        logger.LogInfo($"Marked {inactivePlayers} players as inactive.", caller: nameof(BackgroundTasks));
    }

    public async Task DeleteUnnecessaryScores(CancellationToken ct)
    {
        logger.LogInfo("Deleting old scores...", caller: nameof(BackgroundTasks));
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<IScoresRepository>();

        var deletedScores = await scores.DeleteOldScores();

        foreach (var id in deletedScores)
            File.Delete(Storage.GetReplayPath(id));
        
        logger.LogInfo($"Deleted {deletedScores.Count} replays.", caller: nameof(BackgroundTasks));
    }

    public async Task CheckExpiringSupporters(CancellationToken ct)
    {
        logger.LogInfo("Checking expiring supporter privileges...", caller: nameof(BackgroundTasks));
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();
        
        var expiredSupporters = await players.GetPlayerIdsWithExpiredSupporter();

        foreach (var supporter in expiredSupporters)
        {
            var player = playerService.GetPlayer(supporter);
            if (player == null) continue;
            
            player.Privileges &= ~PlayerPrivileges.Supporter;
            player.RemainingSupporter = DateTime.MinValue;
            
            player.Enqueue(new ServerPackets()
                .Notification("Your supporter status has expired.\nThank you for supporting us!")
                .FinalizeAndGetContent());
            
            logger.LogDebug($"{player.Username}'s supporter status has expired.", caller: nameof(BackgroundTasks));
        }
        
        logger.LogInfo($"Supporter status has expired for {expiredSupporters.Count} players.", caller: nameof(BackgroundTasks));
    }

    public void UpdateBotStatus()
    {
        var random = new Random();
        var botStatuses = AppSettings.BotStatuses;

        foreach (var bot in playerService.Bots)
        {
            var status = botStatuses[random.Next(0, botStatuses.Count)];
            var botStatus = bot.Status;

            botStatus.Activity = status.Activity;
            botStatus.ActivityDescription = status.Description;
        }
    }
}