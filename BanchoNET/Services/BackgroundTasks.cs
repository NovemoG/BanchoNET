using System.Diagnostics;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.History;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
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
        { "AppendPlayerHistory", "0 0 * * *" },         // every day at midnight
        { "MarkInactivePlayers", "0 0 * * *" },         // every day at midnight
        { "DeleteUnnecessaryScores", "0 0 */2 * *" },   // every 2 days at midnight
        { "CleanupRefreshTokens", "0 0 * * *" },        // every day at midnight
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
        
        var jobTasks = _cronMap
            .Select(c => JobLoopAsync(c.Key, c.Value, stoppingToken));
        
        await Task.WhenAll(jobTasks);
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
                
                while (true)
                {
                    var remaining = next.Value - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero) break;

                    await Task.Delay(remaining, stoppingToken);
                }

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

            case "AppendPlayerHistory":
                return AppendPlayerHistory(ct);

            case "MarkInactivePlayers":
                return MarkInactivePlayers(ct);

            case "DeleteUnnecessaryScores":
                return DeleteUnnecessaryScores(ct);

            case "CleanupRefreshTokens":
                return CleanupRefreshTokens(ct);

            default:
                logger.LogWarning($"Unknown cron job name: {jobName}");
                return Task.CompletedTask;
        }
    }
    
    #region Player History
    
    private const int HistoryBatchSize = 2000;
    
    public async Task AppendPlayerHistory(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        
        var monthlyBucket = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
        
        var backfilled = await db.MaintenanceState
            .AsNoTracking()
            .AnyAsync(s => s.Key == IHistoryMaintenanceService.LifetimeCounterBackfillKey, ct);

        if (!backfilled)
        {
            logger.LogWarning(
                "Skipping monthly history samples: lifetime counter backfill has not been applied. " +
                "It runs at startup, so this means the pass failed — check the init logs.",
                caller: nameof(BackgroundTasks)
            );
        }

        logger.LogInfo(
            $"Appending player history (daily {today}, monthly {(backfilled ? monthlyBucket.ToString() : "skipped")})",
            caller: nameof(BackgroundTasks)
        );

        var stopwatch = Stopwatch.StartNew();
        var inserted = 0;

        foreach (var mode in ModeExtensions.TrackedModes)
        {
            if (ct.IsCancellationRequested) break;

            inserted += await ProcessPlayerHistory(mode, today, backfilled ? monthlyBucket : null, ct);
        }

        stopwatch.Stop();

        var state = await db.MaintenanceState
            .FirstOrDefaultAsync(s => s.Key == PlayerHistoryJobKey, ct);

        if (state == null)
        {
            db.MaintenanceState.Add(new MaintenanceStateDto
            {
                Key = PlayerHistoryJobKey,
                LastRunAt = now,
                Details = $"daily={today} monthly={monthlyBucket} inserted={inserted}"
            });
        }
        else
        {
            state.LastRunAt = now;
            state.Details = $"daily={today} monthly={monthlyBucket} inserted={inserted}";
        }

        await db.SaveChangesAsync(ct);

        logger.LogInfo(
            $"Finished appending player history: {inserted} samples in {stopwatch.Elapsed}",
            caller: nameof(BackgroundTasks)
        );
    }

    private const string PlayerHistoryJobKey = "job:player_history";
    
    private async Task<int> ProcessPlayerHistory(
        byte mode,
        DateOnly dailyBucket,
        DateOnly? monthlyBucket,
        CancellationToken ct
    ) {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();
        var playerHistories = scope.ServiceProvider.GetRequiredService<IPlayerHistoryRepository>();

        var stopwatch = Stopwatch.StartNew();

        var ranks = await BuildRankMap(redis, mode);

        var lastPlayerId = 0;
        var inserted = 0;
        var samples = new List<PlayerHistorySample>(HistoryBatchSize * 4);

        while (!ct.IsCancellationRequested)
        {
            var batch = await db.Stats
                .AsNoTracking()
                .Where(s => s.Mode == mode
                            && s.PlayCount > 0
                            && (s.Player.Privileges & 1) == 1
                            && s.PlayerId > lastPlayerId)
                .OrderBy(s => s.PlayerId)
                .Take(HistoryBatchSize)
                .Select(s => new
                {
                    s.PlayerId,
                    s.PP,
                    s.PlayCount,
                    s.ReplayViews
                })
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            lastPlayerId = batch[^1].PlayerId;
            samples.Clear();

            foreach (var row in batch)
            {
                // A player missing from the leaderboard (restricted, or never given a score)
                // gets no rank sample rather than a fabricated one.
                if (ranks.TryGetValue(row.PlayerId, out var rank))
                {
                    samples.Add(new PlayerHistorySample(
                        row.PlayerId, mode, HistoryMetric.GlobalRank, HistoryGranularity.Daily, dailyBucket, rank
                    ));
                }

                samples.Add(new PlayerHistorySample(
                    row.PlayerId, mode, HistoryMetric.Pp, HistoryGranularity.Daily, dailyBucket, row.PP
                ));

                if (monthlyBucket is not { } bucket) continue;
                
                samples.Add(new PlayerHistorySample(
                    row.PlayerId, mode, HistoryMetric.PlayCount, HistoryGranularity.Monthly, bucket, row.PlayCount
                ));

                samples.Add(new PlayerHistorySample(
                    row.PlayerId, mode, HistoryMetric.ReplayViews, HistoryGranularity.Monthly, bucket, row.ReplayViews
                ));
            }

            inserted += await playerHistories.AppendSamples(samples, ct);
        }

        stopwatch.Stop();
        logger.LogDebug(
            $"Player history for mode {mode}: {inserted} samples in {stopwatch.Elapsed}",
            caller: nameof(BackgroundTasks)
        );

        return inserted;
    }
    
    private static async Task<Dictionary<int, int>> BuildRankMap(
        IDatabase redis,
        byte mode
    ) {
        var key = $"bancho:leaderboard:{mode}";
        var total = await redis.SortedSetLengthAsync(key);

        var ranks = new Dictionary<int, int>((int)total);
        const int page = 10_000;

        for (long start = 0; start < total; start += page)
        {
            var entries = await redis.SortedSetRangeByRankAsync(
                key: key,
                start: start,
                stop: start + page - 1,
                order: Order.Descending
            );

            for (var i = 0; i < entries.Length; i++)
            {
                if (int.TryParse((string?)entries[i], out var playerId))
                    ranks[playerId] = (int)(start + i) + 1;
            }
        }

        return ranks;
    }

    #endregion
    
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
        logger.LogInfo("Deleting old scores...", caller: nameof(BackgroundTasks));
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ILegacyScoresRepository>();

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