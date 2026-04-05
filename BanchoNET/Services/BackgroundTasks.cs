using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using Cronos;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BanchoNET.Services;

public class BackgroundTasks : BackgroundService, IBackgroundTasks
{
    private readonly Dictionary<string, string> _cronMap = new()
    {
        { "UpdateBotStatus", $"*/{AppSettings.BotStatusUpdateInterval} * * * *" },
        { "CheckSupporters", "*/30 * * * *" },          // every 30 minutes
        { "AppendRankHistory", "0 0 * * *" },           // every day at midnight
        { "MarkInactivePlayers", "0 0 * * *" },         // every day at midnight
        { "DeleteUnnecessaryScores", "0 0 */2 * *" },   // every 2 days at midnight
        { "AppendMonthlyHistory", "0 0 1 * *" },        // every 1st day of the month at midnight
        { "UpdateLazer", "0 0 * * *" },                 // every day at midnight
    };
    
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPlayerService _playerService;

    public BackgroundTasks(ILogger logger,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IPlayerService playerService
    ) {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _playerService = playerService;
        
        _client = httpClientFactory.CreateClient(nameof(BackgroundTasks));
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json")
        );
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("request")));
        
        if (!string.IsNullOrWhiteSpace(AppSettings.GithubToken))
            _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "token " + AppSettings.GithubToken);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        _logger.LogInfo("Starting background tasks...", caller: nameof(BackgroundTasks));

        try
        {
            UpdateBotStatus();
            await CheckExpiringSupporters(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during initial background tasks run.", ex);
        }

        var updateLazerTask = UpdateLazer(stoppingToken);
        var jobTasks = _cronMap
            .Select(c => JobLoopAsync(c.Key, c.Value, stoppingToken));

        await Task.WhenAll(new[] { updateLazerTask }.Concat(jobTasks));
    }

    private async Task JobLoopAsync(
        string jobName,
        string cronExpression,
        CancellationToken stoppingToken
    ) {
        _logger.LogInfo($"Started cron job loop: {jobName} => {cronExpression}");
        
        var cron = CronExpression.Parse(cronExpression);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var next = cron.GetNextOccurrence(now, TimeZoneInfo.Utc);

                if (next == null)
                {
                    _logger.LogWarning($"No next occurence for {jobName} (cron: {cronExpression})");
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
                _logger.LogError($"Error executing job {jobName}", ex);
            }
        }
        
        _logger.LogInfo($"Stopping cron loop for job {jobName}");
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
            
            case "UpdateLazer":
                return UpdateLazer(ct);

            default:
                _logger.LogWarning($"Unknown cron job name: {jobName}");
                return Task.CompletedTask;
        }
    }

    #region Lazer Updater

    public async Task UpdateLazer(
        CancellationToken ct
    ) {
        if (string.IsNullOrEmpty(AppSettings.GithubToken))
            return;
        
        var lazerVersion = string.Empty;
        var tachyonVersion = string.Empty;
        
        if (File.Exists(Storage.CurrentLazerVersionFile))
        {
            var lazerVersions = (await File.ReadAllTextAsync(Storage.CurrentLazerVersionFile, ct)).Split('\n');

            lazerVersion = lazerVersions.Length > 0 ? lazerVersions[0] : string.Empty;
            tachyonVersion = lazerVersions.Length > 1 ? lazerVersions[1] : string.Empty;
        }

        var latestReleases = await GetLatestMatchingReleases(ct);
        var latestLazerVersion = latestReleases.FirstOrDefault(r => r.TagName.EndsWith("-lazer"))?.TagName;
        var latestTachyonVersion = latestReleases.FirstOrDefault(r => r.TagName.EndsWith("-tachyon"))?.TagName;
        
        if (latestLazerVersion != null && !latestLazerVersion.Equals(lazerVersion))
            await TryReplaceRepo(tachyon: false, latestLazerVersion, ct);

        if (latestTachyonVersion != null && !latestTachyonVersion.Equals(tachyonVersion))
            await TryReplaceRepo(tachyon: true, latestTachyonVersion, ct);

        await File.WriteAllTextAsync(
            Storage.CurrentLazerVersionFile, $"{latestLazerVersion}\n{latestTachyonVersion}", ct
        );
    }

    private async Task TryReplaceRepo(
        bool tachyon,
        string tagName,
        CancellationToken ct
    ) {
        await ReplaceRepo(tachyon, tagName, ct);
        await ReplaceDomains(tachyon, ct);
        await UpdateCsproj(tachyon, ct);
        // create an update file
    }

    private async Task<List<GitHubRelease>> GetLatestMatchingReleases(
        CancellationToken ct
    ) {
        var releases = new List<GitHubRelease>();

        var lazerFound = false;
        var tachyonFound = false;
        for (var page = 1; page <= 10; page++)
        {
            var url = $"https://api.github.com/repos/ppy/osu/releases?per_page=10&page={page}";
            using var doc = await GetJson(url, ct);

            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                break;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var rel = GitHubRelease.FromJson(item);
                if (!lazerFound && rel is { Prerelease: false, Draft: false })
                {
                    lazerFound = true;
                    releases.Add(rel);
                }
                if (!tachyonFound && rel is { Prerelease: true, Draft: false })
                {
                    tachyonFound = true;
                    releases.Add(rel);
                }
            }
            
            if (lazerFound && tachyonFound)
                break;
        }

        return releases;
    }

    private static async Task UpdateCsproj(
        bool tachyon,
        CancellationToken ct
    ) {
        var text = await File.ReadAllTextAsync(Storage.OsuDesktopCsproj(tachyon), ct);
        text = text.Replace("</Project>", "\t<ItemGroup>\n\t<AssemblyAttribute Include=\"osu.Game.Utils.OfficialBuildAttribute\" />\n\t</ItemGroup>\n</Project>");
        await File.WriteAllTextAsync(Storage.OsuDesktopCsproj(tachyon), text, ct);
    }

    private async Task ReplaceRepo(
        bool tachyon,
        string tagName,
        CancellationToken ct
    ) {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ghrel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var zipPath = Path.Combine(tempRoot, "src.zip");
            var zipUrl = $"https://api.github.com/repos/ppy/osu/zipball/{Uri.EscapeDataString(tagName)}";
            await DownloadRepo(zipUrl, zipPath, ct);

            var extractDir = Path.Combine(tempRoot, "extract");
            Directory.CreateDirectory(extractDir);
            await ZipFile.ExtractToDirectoryAsync(zipPath, extractDir, ct);

            var root = Directory.GetDirectories(extractDir).Single();

            var targetDirectory = Path.Combine(Storage.LazerPath, tachyon ? "Tachyon" : "Lazer");
            if (Directory.Exists(targetDirectory))
                Directory.Delete(targetDirectory, recursive: true);

            Directory.CreateDirectory(targetDirectory);
            CopyDirectoryContents(root, targetDirectory);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static void CopyDirectoryContents(
        string sourceDir,
        string targetDir
    ) {
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(targetDir, rel));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var dest = Path.Combine(targetDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private async Task<JsonDocument> GetJson(
        string url,
        CancellationToken ct
    ) {
        
        using var response = await _client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    private async Task DownloadRepo(
        string url,
        string path,
        CancellationToken ct
    ) {
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(path);
        await input.CopyToAsync(output, ct);
    }

    private static async Task ReplaceDomains(
        bool tachyon,
        CancellationToken ct
    ) {
        // Update production endpoint to match ours
        var text = await File.ReadAllTextAsync(Storage.ProductionEndpointPath(tachyon), ct);
        
        text = Regex.Replace(text, @"ppy\.sh\b", AppSettings.Domain);
        text = Regex.Replace(text, @"spectator\.osu\b", "spectator");
        
        await File.WriteAllTextAsync(Storage.ProductionEndpointPath(tachyon), text, ct);

        // Make lazer trust our hostname
        text = await File.ReadAllTextAsync(Storage.TrustedDomainStorePath(tachyon), ct);
        text = text.Replace(
            "if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !uri.Host.EndsWith(@\".ppy.sh\", StringComparison.OrdinalIgnoreCase))",
            $"if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !(uri.Host.EndsWith(@\".ppy.sh\", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(@\".{AppSettings.Domain}\", StringComparison.OrdinalIgnoreCase)))"
        );
        await File.WriteAllTextAsync(Storage.TrustedDomainStorePath(tachyon), text, ct);
    }

    #endregion
    
    #region Rank History

    public async Task AppendPlayerRankHistory(CancellationToken ct)
    {
        _logger.LogInfo($"Appending players' daily rank history ({DateTime.UtcNow})", caller: nameof(BackgroundTasks));

        for (var i = 0; i < 8; i++)
        {
            await ProcessRankHistory((byte)i, ct); //TODO fix with batch updates
        }

        _logger.LogInfo($"Finished updating players' rank history ({DateTime.UtcNow})", caller: nameof(BackgroundTasks));
    }

    private async Task ProcessRankHistory(byte mode, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
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
        _logger.LogInfo($"Finished updating daily rank history for mode {mode}, execution time: {stopwatch.Elapsed}",
            caller: nameof(BackgroundTasks));
    }

    #endregion

    #region Monthly History

    public async Task AppendPlayerMonthlyHistory(CancellationToken ct)
    {
        _logger.LogInfo($"Appending players' monthly history ({DateTime.UtcNow})", caller: nameof(BackgroundTasks));

        for (var i = 0; i < 8; i++)
        {
            await ProcessMonthlyHistory((byte)i, ct); //TODO fix with batch updates
        }
        
        _logger.LogInfo($"Finished updating players' monthly history ({DateTime.UtcNow})", caller: nameof(BackgroundTasks));
    }
    
    private async Task ProcessMonthlyHistory(byte mode, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
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
        _logger.LogInfo($"Finished updating monthly history for mode {mode}, execution time: {stopwatch.Elapsed}",
            caller: nameof(BackgroundTasks));
    }

    #endregion
    
    public async Task MarkInactivePlayers(CancellationToken ct)
    {
        _logger.LogInfo("Marking inactive players...", caller: nameof(BackgroundTasks));
        
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BanchoDbContext>();
        
        var inactivePlayers = await db.Players
            .Where(p => !p.Inactive && p.LastActivityTime < DateTime.UtcNow.AddDays(-AppSettings.DaysUntilPlayerIsMarkedInactive))
            .ExecuteUpdateAsync(p => p.SetProperty(u => u.Inactive, true), cancellationToken: ct);
        
        _logger.LogInfo($"Marked {inactivePlayers} players as inactive.", caller: nameof(BackgroundTasks));
    }

    public async Task DeleteUnnecessaryScores(CancellationToken ct)
    {
        _logger.LogInfo("Deleting old scores...", caller: nameof(BackgroundTasks));
        
        await using var scope = _scopeFactory.CreateAsyncScope();
        var scores = scope.ServiceProvider.GetRequiredService<ILegacyScoresRepository>();

        var deletedScores = await scores.DeleteOldScores();

        foreach (var id in deletedScores)
            File.Delete(Storage.GetReplayPath(id));
        
        _logger.LogInfo($"Deleted {deletedScores.Count} replays.", caller: nameof(BackgroundTasks));
    }

    public async Task CheckExpiringSupporters(CancellationToken ct)
    {
        _logger.LogInfo("Checking expiring supporter privileges...", caller: nameof(BackgroundTasks));
        
        await using var scope = _scopeFactory.CreateAsyncScope();
        var players = scope.ServiceProvider.GetRequiredService<IPlayersRepository>();
        
        var expiredSupporters = await players.GetPlayerIdsWithExpiredSupporter();

        foreach (var supporter in expiredSupporters)
        {
            var player = _playerService.GetPlayer(supporter);
            if (player == null) continue;
            
            player.Privileges &= ~PlayerPrivileges.Supporter;
            player.RemainingSupporter = DateTime.MinValue;
            
            player.Enqueue(new ServerPackets()
                .Notification("Your supporter status has expired.\nThank you for supporting us!")
                .FinalizeAndGetContent());
            
            _logger.LogDebug($"{player.Username}'s supporter status has expired.", caller: nameof(BackgroundTasks));
        }
        
        _logger.LogInfo($"Supporter status has expired for {expiredSupporters.Count} players.", caller: nameof(BackgroundTasks));
    }

    public void UpdateBotStatus()
    {
        var random = new Random();
        var botStatuses = AppSettings.BotStatuses;

        foreach (var bot in _playerService.Bots)
        {
            var status = botStatuses[random.Next(0, botStatuses.Count)];
            var botStatus = bot.Status;

            botStatus.Activity = status.Activity;
            botStatus.ActivityDescription = status.Description;
        }
    }
}