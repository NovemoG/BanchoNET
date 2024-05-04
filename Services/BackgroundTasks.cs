using System.Diagnostics;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils;
using Hangfire;

namespace BanchoNET.Services;

public class BackgroundTasks
{
    private readonly BanchoSession _session = BanchoSession.Instance;
    private readonly IServiceScopeFactory _scopeFactory;

    public BackgroundTasks(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        
        RecurringJob.AddOrUpdate(
            "updateBotStatus",
            () => UpdateBotStatus(),
            $"*/{AppSettings.BotStatusUpdateInterval} * * * *"); // every x minutes
        
        RecurringJob.AddOrUpdate(
            "deleteScores",
            () => DeleteUnnecessaryScores(),
            "0 0 */2 * *"); // every 2 days at midnight
        
        RecurringJob.AddOrUpdate(
            "checkSupporters",
            () => CheckExpiringSupporters(),
            "*/30 * * * *"); // every 30 minutes
    }
    
    public BackgroundTasks() : this(null!) { }

    public async Task InitTasks()
    {
        Console.WriteLine($"[{GetType().Name}] Initiating background tasks, execution date: {DateTime.Now}");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        UpdateBotStatus();
        await CheckExpiringSupporters();
        
        stopwatch.Stop();
        Console.WriteLine($"[{GetType().Name}] Initiating background tasks, execution time: {stopwatch.Elapsed}");
    }

    public async Task DeleteUnnecessaryScores()
    {
        Console.WriteLine($"[{GetType().Name}] Deleting old scores...");
        
        await using var scope = _scopeFactory.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ScoresRepository>();

        var deletedScores = await scores.DeleteOldScores();
        Console.WriteLine($"[{GetType().Name}] Deleted {deletedScores} scores.");
    }

    public async Task CheckExpiringSupporters()
    {
        Console.WriteLine($"[{GetType().Name}] Updating expiring supporters privileges...");
        
        await using var scope = _scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<PlayersRepository>();
        
        var expiredSupporters = await players.GetPlayersWithExpiredSupporter();

        foreach (var supporter in expiredSupporters)
        {
            var player = _session.GetPlayer(id: supporter);
            if (player == null) continue;
            
            player.Privileges &= ~Privileges.Supporter;
            player.RemainingSupporter = DateTime.MinValue;

            using var notificationPacket = new ServerPackets();
            notificationPacket.Notification("Your supporter status has expired.\nThank you for supporting us!");
            player.Enqueue(notificationPacket.GetContent());
            
            Console.WriteLine($"[{GetType().Name}] {player.Username}'s supporter status has expired.");
        }
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