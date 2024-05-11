using System.Diagnostics;
using BanchoNET.Models.Mongo;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils;
using Hangfire;
using StackExchange.Redis;

namespace BanchoNET.Services;

public class BackgroundTasks(IServiceScopeFactory scopeFactory)
{
    private readonly BanchoSession _session = BanchoSession.Instance;

    public BackgroundTasks() : this(null!) { }

    public async Task Init()
    {
        Console.WriteLine($"[Init] Initiating background tasks, execution date: {DateTime.Now}");
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
            "deleteScores",
            () => AppendPlayerRankHistory(),
            "0 0 * * *"); // every day at midnight
        
        RecurringJob.AddOrUpdate(
            "deleteScores",
            () => DeleteUnnecessaryScores(),
            "0 0 */2 * *"); // every 2 days at midnight
        
        RecurringJob.AddOrUpdate(
            "checkSupporters",
            () => ClearPasswordsCache(),
            "0 0 1 * *"); // every 1st day of the month at midnight
        
        RecurringJob.AddOrUpdate(
            "deleteScores",
            () => AppendPlayerMonthlyHistory(),
            "0 0 1 * *"); // every 1st day of the month at midnight
        
        stopwatch.Stop();
        Console.WriteLine($"[Init] Initiating background tasks, execution time: {stopwatch.Elapsed}");
    }

    public void ClearPasswordsCache()
    {
        Console.WriteLine($"[{GetType().Name}] Clearing passwords cache.");
        
        _session.ClearPasswordsCache();
    }

    #region Rank History

    public async Task AppendPlayerRankHistory()
    {
        Console.WriteLine($"[{GetType().Name}] Appending players' daily rank history, execution date: {DateTime.Now})");
        
        await Parallel.ForAsync(0, 8, async (i, _) => await ProcessRankHistory((byte)i));
        
        Console.WriteLine($"[{GetType().Name}] Finished updating players' rank history, finish date: {DateTime.Now}");
    }

    private async Task ProcessRankHistory(byte mode)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<PlayersRepository>();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var histories = scope.ServiceProvider.GetRequiredService<HistoriesRepository>();
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var playerCount = await players.TotalPlayerCount();
        const int limit = 10_000;
        
        mode = mode == 7 ? (byte)(mode + 1) : mode;
        var key = $"bancho:leaderboard:{mode}";
            
        var iter = 0;
        for (int i = 0; i < playerCount; i += limit)
        {
            var ranks = await redis.SortedSetRangeByRankWithScoresAsync(
                key: key,
                start: i,
                stop: limit + iter * i - 1,
                order: Order.Descending);
                
            for (int j = 0; j < ranks.Length; j++)
            {
                var playerId = int.Parse(ranks[j].Element!);

                await histories.AddRankHistory(
                    playerId,
                    mode,
                    new ValueEntry
                    {
                        Value = i + j + 1,
                        Date = DateTime.Now
                    });
            }
                
            iter++;
        }
        
        stopwatch.Stop();
        Console.WriteLine($"[{GetType().Name}] Finished updating daily rank history for mode {mode}, execution time: {stopwatch.Elapsed}");
    }

    #endregion

    #region Monthly History

    public async Task AppendPlayerMonthlyHistory()
    {
        Console.WriteLine($"[{GetType().Name}] Appending players' monthly history, execution date: {DateTime.Now})");
        
        await Parallel.ForAsync(0, 8, async (i, _) => await ProcessMonthlyHistory((byte)i));
        
        Console.WriteLine($"[{GetType().Name}] Finished updating monthly history, finish date: {DateTime.Now}");
    }
    
    private async Task ProcessMonthlyHistory(byte mode)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<PlayersRepository>();
        var histories = scope.ServiceProvider.GetRequiredService<HistoriesRepository>();
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var playerCount = await players.TotalPlayerCount();
        const int limit = 10_000;
        
        mode = mode == 7 ? (byte)(mode + 1) : mode;
        
        var iter = 0;
        for (int i = 0; i < playerCount; i += limit)
        {
            var stats = await players.GetPlayerModeStatsInRange(mode, limit, iter * i);
            
            foreach (var (playerId, playCount, replaysViewed) in stats)
            {
                await Task.WhenAll(
                    histories.AddPlayCountHistory(
                        playerId,
                        mode,
                        new ValueEntry
                        {
                            Value = playCount,
                            Date = DateTime.Now
                        }),
                    histories.AddReplaysHistory(
                        playerId,
                        mode,
                        new ValueEntry
                        {
                            Value = replaysViewed,
                            Date = DateTime.Now
                        })
                );
            }
                
            iter++;
        }

        await players.ResetPlayersStats(mode);
        
        stopwatch.Stop();
        Console.WriteLine($"[{GetType().Name}] Finished updating monthly history for mode {mode}, execution time: {stopwatch.Elapsed}");
    }

    #endregion

    public async Task DeleteUnnecessaryScores()
    {
        Console.WriteLine($"[{GetType().Name}] Deleting old scores...");
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoresRepository>();

        var deletedScores = await scores.DeleteOldScores();

        foreach (var id in deletedScores)
            File.Delete(Storage.GetReplayPath(id));
        
        Console.WriteLine($"[{GetType().Name}] Deleted {deletedScores.Count} replays.");
    }

    public async Task CheckExpiringSupporters()
    {
        Console.WriteLine($"[{GetType().Name}] Updating expiring supporters privileges...");
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<PlayersRepository>();
        
        var expiredSupporters = await players.GetPlayersWithExpiredSupporter();

        foreach (var supporter in expiredSupporters)
        {
            var player = _session.GetPlayerById(supporter);
            if (player == null) continue;
            
            player.Privileges &= ~Privileges.Supporter;
            player.RemainingSupporter = DateTime.MinValue;

            using var notificationPacket = new ServerPackets();
            notificationPacket.Notification("Your supporter status has expired.\nThank you for supporting us!");
            player.Enqueue(notificationPacket.GetContent());
            
            Console.WriteLine($"[{GetType().Name}] {player.Username}'s supporter status has expired.");
        }
        
        Console.WriteLine($"[{GetType().Name}] Supporter status has expired for {expiredSupporters.Count} players.");
    }

    public void UpdateBotStatus()
    {
        var random = new Random();
        var botStatuses = AppSettings.BotStatuses;

        foreach (var bot in _session.Bots)
        {
            var status = botStatuses[random.Next(0, botStatuses.Count)];
            var botStatus = bot.Status;

            botStatus.Activity = status.Activity;
            botStatus.ActivityDescription = status.Description;
        }
    }
}