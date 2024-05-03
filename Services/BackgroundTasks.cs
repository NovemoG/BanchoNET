using System.Diagnostics;
using BanchoNET.Utils;
using Hangfire;

namespace BanchoNET.Services;

public class BackgroundTasks
{
    private readonly BanchoSession _session = BanchoSession.Instance;
    
    public BackgroundTasks()
    {
        RecurringJob.AddOrUpdate(
            "updateBotStatus",
            () => UpdateBotStatus(),
            $"*/{AppSettings.BotStatusUpdateInterval} * * * *");
    }
    
    public async Task InitTasks()
    {
        Console.WriteLine($"[{GetType().Name}] Initiating background tasks, execution date: {DateTime.Now}");
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        UpdateBotStatus();
        
        stopwatch.Stop();
        Console.WriteLine($"[{GetType().Name}] Initiating background tasks, execution time: {stopwatch.Elapsed}");
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