using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Packets;
using System.Collections.Concurrent;
using BanchoNET.Core.Utils;
using Cronos;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Services;

public class BackgroundTasks(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    IPlayerService playerService
) : BackgroundService, IBackgroundTasks
{
    private sealed record CronJob(
        string Name,
        string Schedule,
        Func<CancellationToken, Task> Run
    );
    
    private CronJob[] Jobs =>
    [
        new("UpdateBotStatus", $"*/{AppSettings.BotStatusUpdateInterval} * * * *", _ =>
        {
            UpdateBotStatus();
            return Task.CompletedTask;
        }),
        new("SampleOnlineCount", "*/10 * * * *", SampleOnlineCount),               // every 10 minutes
        new("CheckSupporters", "*/30 * * * *", CheckExpiringSupporters),           // every 30 minutes
        new("AppendPlayerHistory", "0 0 * * *", AppendPlayerHistory),              // every day at midnight
        new("MarkInactivePlayers", "0 0 * * *", MarkInactivePlayers),              // every day at midnight
        new("DeleteUnnecessaryScores", "0 0 */2 * *", DeleteUnnecessaryScores),    // every 2 days at midnight
        new("RemindAboutOsuCookie", "*/15 * * * *", RemindOwnersAboutOsuCookie),   // every 15 minutes
        new("CleanupRefreshTokens", "0 0 * * *", CleanupRefreshTokens),            // every day at midnight
    ];

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
        
        var jobTasks = Jobs.Select(job => JobLoopAsync(job, stoppingToken));

        await Task.WhenAll(jobTasks);
    }

    private async Task JobLoopAsync(
        CronJob job,
        CancellationToken stoppingToken
    ) {
        logger.LogInfo($"Started cron job loop: {job.Name} => {job.Schedule}");

        var cron = CronExpression.Parse(job.Schedule);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var next = cron.GetNextOccurrence(now, TimeZoneInfo.Utc);

                if (next == null)
                {
                    logger.LogWarning($"No next occurence for {job.Name} (cron: {job.Schedule})");
                    return;
                }

                while (true)
                {
                    var remaining = next.Value - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero) break;

                    await Task.Delay(remaining, stoppingToken);
                }

                await job.Run(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error executing job {job.Name}", ex);
            }
        }

        logger.LogInfo($"Stopping cron loop for job {job.Name}");
    }
    
    public async Task AppendPlayerHistory(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var collector = scope.ServiceProvider.GetRequiredService<IPlayerHistoryCollector>();

        await collector.Collect(ct);
    }

    public async Task SampleOnlineCount(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var stats = scope.ServiceProvider.GetRequiredService<IServerStatsService>();
        
        await stats.SampleOnlineCount();
    }
    
    private static readonly TimeSpan CookieReminderInterval = TimeSpan.FromDays(1);
    private readonly ConcurrentDictionary<int, DateTimeOffset> _cookieRemindedAt = new();
    
    public async Task RemindOwnersAboutOsuCookie(CancellationToken ct)
    {
        var reminder = OsuSessionCookie.GetReminder();
        if (reminder == null)
        {
            _cookieRemindedAt.Clear();
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotifySocketManager>();

        const int owner = (int)PlayerPrivileges.Owner;
        var ownerIds = await db.Players
            .Where(p => (p.Privileges & owner) == owner)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;

        foreach (var ownerId in ownerIds)
        {
            if (_cookieRemindedAt.TryGetValue(ownerId, out var remindedAt) && now - remindedAt < CookieReminderInterval)
                continue;

            var delivered = false;

            if (notifications.IsConnected(ownerId))
            {
                await notifications.SendAsync(ownerId, "novemo.server.notice", new { Message = reminder, Important = true }, ct);
                delivered = true;
            }

            var stablePlayer = playerService.GetPlayer(ownerId);
            if (stablePlayer is { IsOnlineOnStable: true })
            {
                stablePlayer.Enqueue(new ServerPackets().Notification(reminder).FinalizeAndGetContent());
                delivered = true;
            }

            if (delivered)
                _cookieRemindedAt[ownerId] = now;
        }
    }

    public async Task CleanupRefreshTokens(CancellationToken ct)
    {
        logger.LogInfo("Cleaning up refresh tokens...", caller: nameof(BackgroundTasks));

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();

        var now = DateTime.UtcNow;
        
        var deleted = await db.RefreshTokens
            .Where(t => t.ExpiresAt < now)
            .ExecuteDeleteAsync(ct);

        logger.LogInfo($"Deleted {deleted} expired refresh tokens.", caller: nameof(BackgroundTasks));
    }

    public async Task MarkInactivePlayers(CancellationToken ct)
    {
        logger.LogInfo("Marking inactive players...", caller: nameof(BackgroundTasks));
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
        
        var inactivePlayers = await db.Players
            .Where(p => !p.Inactive && p.LastActivityTime < DateTime.UtcNow.AddDays(-AppSettings.DaysUntilPlayerIsMarkedInactive))
            .ExecuteUpdateAsync(p => p.SetProperty(u => u.Inactive, true), cancellationToken: ct);
        
        logger.LogInfo($"Marked {inactivePlayers} players as inactive.", caller: nameof(BackgroundTasks));
    }

    public async Task DeleteUnnecessaryScores(CancellationToken ct)
    {
        logger.LogInfo($"Applying score retention ({AppSettings.ScoreRetentionMode})...", caller: nameof(BackgroundTasks));

        await using var scope = scopeFactory.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<IScoresRepository>();

        var expiredReplays = await scores.PurgeOldScores();
        var removed = 0;

        foreach (var id in expiredReplays)
        {
            var path = Storage.GetReplayPath(id);
            if (!File.Exists(path)) continue;

            File.Delete(path);
            removed++;
        }

        logger.LogInfo($"Deleted {removed} replays.", caller: nameof(BackgroundTasks));
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