using System.Diagnostics;
using BanchoNET.Abstractions.Repositories;
using BanchoNET.Abstractions.Repositories.Histories;
using BanchoNET.Abstractions.Services;
using BanchoNET.Models;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BanchoNET.Services;

public class BackgroundTasks(
    IBanchoSession session,
    ILogger logger,
    IServiceScopeFactory scopeFactory
    ) : IBackgroundTasks
{
    public async Task Init()
    {
        logger.LogInfo("Initiating background tasks...");
        var stopwatch = new Stopwatch();
        stopwatch.Start();
         
        UpdateBotStatus();
        await CheckExpiringSupporters();
        
        RecurringJob.AddOrUpdate(
            "updateBotStatus",
            () => UpdateBotStatus(),
            $"*/{AppSettings.BotStatusUpdateInterval} * * * *"); // every x minutes
        
        RecurringJob.AddOrUpdate(
            "checkSupporters",
            () => CheckExpiringSupporters(),
            "*/30 * * * *"); // every 30 minutes
        
        RecurringJob.AddOrUpdate(
            "appendRankHistory",
            () => AppendPlayerRankHistory(),
            "0 0 * * *"); // every day at midnight
        
        RecurringJob.AddOrUpdate(
            "markInactivePlayers",
            () => MarkInactivePlayers(),
            "0 0 * * *"); // every day at midnight
        
        RecurringJob.AddOrUpdate(
            "deleteScores",
            () => DeleteUnnecessaryScores(),
            "0 0 */2 * *"); // every 2 days at midnight
        
        RecurringJob.AddOrUpdate(
            "clearCache",
            () => ClearPasswordsCache(),
            "0 0 1 * *"); // every 1st day of the month at midnight
        
        RecurringJob.AddOrUpdate(
            "appendMonthlyHistory",
            () => AppendPlayerMonthlyHistory(),
            "0 0 1 * *"); // every 1st day of the month at midnight
        
        stopwatch.Stop();
        logger.LogInfo($"Initiated background tasks in {stopwatch.Elapsed}");
    }

    public void ClearPasswordsCache()
    {
        logger.LogInfo("Clearing passwords cache.", caller: nameof(BackgroundTasks));
        
        session.ClearPasswordsCache();
    }

    #region Rank History

    public async Task AppendPlayerRankHistory()
    {
        logger.LogInfo($"Appending players' daily rank history ({DateTime.Now})", caller: nameof(BackgroundTasks));
        
        await Parallel.ForAsync(0, 8, async (i, _) => await ProcessRankHistory((byte)i));
        
        logger.LogInfo($"Finished updating players' rank history ({DateTime.Now})", caller: nameof(BackgroundTasks));
    }

    private async Task ProcessRankHistory(byte mode)
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
        for (int i = 0; i < playerCount; i += limit)
        {
            var ranks = await redis.SortedSetRangeByRankWithScoresAsync(
                key: key,
                start: i,
                stop: limit + iter * limit - 1,
                order: Order.Descending);
                
            for (int j = 0; j < ranks.Length; j++)
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

    public async Task AppendPlayerMonthlyHistory()
    {
        logger.LogInfo($"Appending players' monthly history ({DateTime.Now})", caller: nameof(BackgroundTasks));
        
        await Parallel.ForAsync(0, 8, async (i, _) => await ProcessMonthlyHistory((byte)i));
        
        logger.LogInfo($"Finished updating players' monthly history ({DateTime.Now})", caller: nameof(BackgroundTasks));
    }
    
    private async Task ProcessMonthlyHistory(byte mode)
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
    
    public async Task MarkInactivePlayers()
    {
        logger.LogInfo("Marking inactive players...", caller: nameof(BackgroundTasks));
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
        
        var inactivePlayers = await db.Players              //TODO make it configurable
            .Where(p => !p.Inactive && p.LastActivityTime < DateTime.Now.AddDays(-14))
            .ExecuteUpdateAsync(p => p.SetProperty(u => u.Inactive, true));
        
        logger.LogInfo($"Marked {inactivePlayers} players as inactive.", caller: nameof(BackgroundTasks));
    }
    
    public async Task DeleteUnnecessaryScores()
    {
        logger.LogInfo("Deleting old scores...", caller: nameof(BackgroundTasks));
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<IScoresRepository>();

        var deletedScores = await scores.DeleteOldScores();

        foreach (var id in deletedScores)
            File.Delete(Storage.GetReplayPath(id));
        
        logger.LogInfo($"Deleted {deletedScores.Count} replays.", caller: nameof(BackgroundTasks));
    }

    public async Task CheckExpiringSupporters()
    {
        logger.LogInfo("Checking expiring supporter privileges...", caller: nameof(BackgroundTasks));
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();
        
        var expiredSupporters = await players.GetPlayerIdsWithExpiredSupporter();

        foreach (var supporter in expiredSupporters)
        {
            var player = session.GetPlayerById(supporter);
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

        foreach (var bot in session.Bots)
        {
            var status = botStatuses[random.Next(0, botStatuses.Count)];
            var botStatus = bot.Status;

            botStatus.Activity = status.Activity;
            botStatus.ActivityDescription = status.Description;
        }
    }
}