using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BanchoNET.Handlers.Lazer.Services;

public class LazerUpdaterService(
    ILogger logger,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory
) : BackgroundService
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(nameof(LazerUpdaterService));
    
    private static readonly (LazerPlatform Platform, string Rid, string EntryExe)[] Platforms =
    [
        (LazerPlatform.Win,   "win-x64",   "osu!.exe"),
        (LazerPlatform.Linux, "linux-x64", "osu!"),
    ];

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        try
        {
            await UpdateLazer(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ignore during startup
        }
        catch (Exception ex)
        {
            logger.LogError("Error while trying to update lazer files", ex, caller: nameof(LazerUpdaterService));
        }

        // everyday at midnight
        var cron = CronExpression.Parse("0 0 * * *");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var next = cron.GetNextOccurrence(now, TimeZoneInfo.Utc);

                if (next == null)
                {
                    logger.LogWarning("Failed to get next cron occurence for lazer updater", caller: nameof(LazerUpdaterService));
                    return;
                }

                var delay = next.Value - now;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);
                
                await UpdateLazer(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError("Error while updating lazer files", ex, caller: nameof(LazerUpdaterService));
            }
        }
    }

    private async Task UpdateLazer(
        CancellationToken ct
    ) {
        if (string.IsNullOrEmpty(AppSettings.GithubToken))
            return;
        
        string? lastLazerTag = null;
        string? lastTachyonTag = null;

        if (File.Exists(LazerStorage.CurrentLazerVersionFile))
        {
            var lines = await File.ReadAllLinesAsync(LazerStorage.CurrentLazerVersionFile, ct);
            lastLazerTag = lines.ElementAtOrDefault(0);
            lastTachyonTag = lines.ElementAtOrDefault(1);
        }
        
        var latestReleases = await GetLatestMatchingReleases(lastLazerTag, lastTachyonTag, ct);
        if (latestReleases.Count == 0)
        {
            logger.LogInfo("Lazer files are up to date");
            return;
        }
        
        Directory.CreateDirectory(LazerStorage.ReleasesPath);

        await using var scope = scopeFactory.CreateAsyncScope();
        var releasesRepo = scope.ServiceProvider.GetRequiredService<IReleasesRepository>();

        var currentLazerTag = lastLazerTag;
        var currentTachyonTag = lastTachyonTag;

        var sawTachyon = false;

        for (var i = latestReleases.Count - 1; i >= 0; i--)
        {
            var rel = latestReleases[i];
            var isTachyon = IsTachyonRelease(rel);
            var tagName = rel.TagName;

            var success = await TryReplaceRepo(tagName, ct);
            if (!success)
            {
                logger.LogWarning($"Failed to replace repo for {tagName}");
                continue;
            }
            
            var winRelease = await LatestReleaseForPlatform(isTachyon, tagName, LazerPlatform.Win, ct);
            await LatestReleaseForPlatform(isTachyon, tagName, LazerPlatform.Linux, ct);

            await releasesRepo.InsertRelease(prerelease: isTachyon, winRelease.Full, winRelease.Delta);

            if (isTachyon)
            {
                currentTachyonTag = tagName;
                sawTachyon = true;
            }
            else
            {
                currentLazerTag = tagName;
                if (!sawTachyon)
                    currentTachyonTag = tagName;
            }
        }
        
        var winSetup = Path.Combine(LazerStorage.WinPackPath, "Releases", $"{AppSettings.LazerName}-win-Setup.exe");
        
        if (File.Exists(winSetup))
            File.Delete(winSetup);
        
        if (Directory.Exists(LazerStorage.LazerPath))
            Directory.Delete(LazerStorage.LazerPath, recursive: true);
        
        await File.WriteAllLinesAsync(
            LazerStorage.CurrentLazerVersionFile,
            [
                currentLazerTag ?? string.Empty,
                currentTachyonTag ?? string.Empty
            ],
            ct
        );
        
        logger.LogInfo("Finished updating lazer to the newest version");
    }
    
    private static bool IsLazerRelease(GitHubRelease rel) =>
        rel is { Draft: false, Prerelease: false };

    private static bool IsTachyonRelease(GitHubRelease rel) =>
        rel is { Draft: false, Prerelease: true };
    
    private async Task<(VelopackAsset Full, VelopackAsset? Delta)> LatestReleaseForPlatform(
        bool tachyon,
        string tagName,
        LazerPlatform platform,
        CancellationToken ct
    ) {
        var generatedFeedPath = LazerStorage.GetGeneratedReleasesFilePath(platform);
        var storedFeedPath = LazerStorage.GetReleasesPath(tagName, platform);

        File.Move(generatedFeedPath, storedFeedPath, overwrite: true);
        
        VelopackReleaseFeed originalFeed;
        {
            await using var fs0 = File.OpenRead(storedFeedPath);
            originalFeed = await JsonSerializer.DeserializeAsync<VelopackReleaseFeed>(fs0, cancellationToken: ct)
                           ?? throw new InvalidOperationException($"Unable to deserialize {storedFeedPath}");
        }

        var originalFullFileName = originalFeed.Assets
            .First(a => a.Type.Equals("Full",  StringComparison.OrdinalIgnoreCase)).FileName;
        var originalDeltaFileName = originalFeed.Assets
            .FirstOrDefault(a => a.Type.Equals("Delta", StringComparison.OrdinalIgnoreCase))?.FileName;
        
        if (platform != LazerPlatform.Win)
        {
            var feedText = await File.ReadAllTextAsync(storedFeedPath, ct);
            feedText = feedText.Replace("-full.nupkg", $"-{platform.ToFileName()}-full.nupkg");
            feedText = feedText.Replace("-delta.nupkg", $"-{platform.ToFileName()}-delta.nupkg");
            await File.WriteAllTextAsync(storedFeedPath, feedText, ct);
        }
        
        VelopackReleaseFeed feed;
        {
            await using var fs = File.OpenRead(storedFeedPath);
            feed = await JsonSerializer.DeserializeAsync<VelopackReleaseFeed>(fs, cancellationToken: ct)
                   ?? throw new InvalidOperationException($"Unable to deserialize {storedFeedPath}");
        }
        
        var generatedPortable = LazerStorage.GetGeneratedPortablePath(platform);
        if (File.Exists(generatedPortable))
            File.Move(generatedPortable, LazerStorage.GetLazerPortablePath(tachyon, platform), overwrite: true);
        
        var latestFull = feed.Assets.First(a => a.Type.Equals("Full", StringComparison.OrdinalIgnoreCase));
        var latestDelta = feed.Assets.FirstOrDefault(a => a.Type.Equals("Delta", StringComparison.OrdinalIgnoreCase));
        
        CopyNupkgToServingDir(platform, originalFullFileName, tagName, "full");
        if (originalDeltaFileName != null)
            CopyNupkgToServingDir(platform, originalDeltaFileName, tagName, "delta");

        return (latestFull, latestDelta);
    }

    private void CopyNupkgToServingDir(
        LazerPlatform platform,
        string sourceFileName,
        string tagName,
        string type
    ) {
        var sourcePath = Path.Combine(LazerStorage.GetPackPath(platform), "Releases", sourceFileName);
        var destPath   = LazerStorage.GetReleaseFilePath(tagName, type, platform);

        if (!File.Exists(sourcePath))
        {
            logger.LogWarning($"Nupkg not found for {platform.ToFileName()}: {sourcePath}");
            return;
        }

        FileLinking.LinkOrCopy(sourcePath, destPath);
    }
    
    private async Task<bool> TryReplaceRepo(
        string tagName,
        CancellationToken ct
    ) {
        await ReplaceRepo(tagName, ct);
        await ReplaceDomains(ct);
        await UpdateCsproj(ct);

        foreach (var (platform, rid, entryExe) in Platforms)
        {
            var platformName = platform.ToFileName();

            logger.LogInfo($"Building lazer project for {platformName}...");
            var stopwatch = Stopwatch.StartNew();

            var publishPath = LazerStorage.GetLazerPublishPath(platform);
            var buildResult = await RunCommand(
                "/opt/dotnet8/dotnet",
                $"publish --self-contained -r {rid} -p:Version={tagName} -o \"{publishPath}\"",
                LazerStorage.LazerProjectPath,
                ct
            );

            if (buildResult.ExitCode != 0)
            {
                logger.LogWarning($"Failed to build lazer for {platformName}");
                logger.LogWarning(buildResult.StdErr);
                return false;
            }

            stopwatch.Stop();
            logger.LogInfo($"Finished building lazer for {platformName} in {stopwatch.Elapsed}");

            logger.LogInfo($"Packing a new version of lazer for {platformName}...");
            stopwatch.Restart();

            var packWorkingDir = LazerStorage.GetPackPath(platform);
            Directory.CreateDirectory(packWorkingDir);

            var iconFlag = platform == LazerPlatform.Win ? $"-i \"{LazerStorage.IconPath}\" " : "";

            var packResult = await RunCommand(
                "/opt/tools/vpk",
                $"[{platformName}] pack --runtime {rid} --packTitle {AppSettings.LazerName} -u {AppSettings.LazerName} {iconFlag}-v {tagName} -p \"{publishPath}\" -e \"{entryExe}\"",
                packWorkingDir,
                ct
            );

            if (packResult.ExitCode != 0)
            {
                logger.LogWarning($"Failed to pack lazer update for {platformName}");
                logger.LogWarning(packResult.StdErr);
                return false;
            }

            logger.LogInfo($"Finished packing lazer for {platformName} in {stopwatch.Elapsed}");
        }
        
        logger.LogInfo($"Updated lazer files to latest version: {tagName}");
        return true;
    }
    
    private static async Task<(int ExitCode, string StdErr)> RunCommand(
        string filename,
        string arguments,
        string workingDirectory,
        CancellationToken ct
    ) {
        var psi = new ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = psi;
        process.EnableRaisingEvents = true;

        var stderr = new StringBuilder();
        
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stderr.AppendLine(e.Data);
        };
        
        if (!process.Start())
            throw new InvalidOperationException($"Failed to start process: {filename}");
        
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);

        return (process.ExitCode, stderr.ToString());
    }

    private async Task<List<GitHubRelease>> GetLatestMatchingReleases(
        string? lastLazerTag,
        string? lastTachyonTag,
        CancellationToken ct
    ) {
        var releases = new List<GitHubRelease>();
        
        // tachyon might be the same as lazer if lazer is newest
        var firstRun = string.IsNullOrWhiteSpace(lastLazerTag);
        var cutoffReached = false;
        
        for (var page = 1; page <= 10 && !cutoffReached; page++)
        {
            var url = $"https://api.github.com/repos/ppy/osu/releases?per_page=10&page={page}";
            using var doc = await GetJson(url, ct);

            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                break;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var rel = GitHubRelease.FromJson(item);

                var isLazer = IsLazerRelease(rel);
                var isTachyon = IsTachyonRelease(rel);

                if (firstRun)
                {
                    if (isTachyon)
                    {
                        releases.Add(rel);
                        continue;
                    }

                    if (isLazer)
                    {
                        releases.Add(rel);
                        cutoffReached = true;
                        break;
                    }
                }

                if (rel.TagName == lastLazerTag || rel.TagName == lastTachyonTag)
                {
                    cutoffReached = true;
                    break;
                }
                
                if (isLazer || isTachyon)
                    releases.Add(rel);
            }
        }

        return releases;
    }

    private static async Task UpdateCsproj(
        CancellationToken ct
    ) {
        await ReplaceInFile(
            LazerStorage.OsuDesktopCsproj,
            "</Project>",
            "\t<ItemGroup>\n\t<AssemblyAttribute Include=\"osu.Game.Utils.OfficialBuildAttribute\" />\n\t</ItemGroup>\n</Project>",
            ct
        );
    }

    private async Task ReplaceRepo(
        string tagName,
        CancellationToken ct
    ) {
        var tempRoot = Path.Combine(Storage.LazerTempPath, "ghrel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var zipPath = Path.Combine(tempRoot, "src.zip");
            var zipUrl = $"https://api.github.com/repos/ppy/osu/zipball/{Uri.EscapeDataString(tagName)}";
            await DownloadRepo(zipUrl, zipPath, ct);

            var extractDir = Path.Combine(tempRoot, "extract");
            Directory.CreateDirectory(extractDir);
            await ZipFile.ExtractToDirectoryAsync(zipPath, extractDir, ct);

            var root = Directory.EnumerateDirectories(extractDir).Single();

            var targetDirectory = LazerStorage.LazerPath;
            if (Directory.Exists(targetDirectory))
                Directory.Delete(targetDirectory, recursive: true);

            Directory.Move(root, targetDirectory);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
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
        logger.LogInfo("Downloading lazer repo...");
        
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var input = await response.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(path);
        await input.CopyToAsync(output, ct);
    }

    private static async Task ReplaceDomains(
        CancellationToken ct
    ) {
        // Update production endpoint to match ours
        var text = await File.ReadAllTextAsync(LazerStorage.ProductionEndpointPath, ct);
        
        text = Regex.Replace(text, @"ppy\.sh\b", AppSettings.Domain);
        text = Regex.Replace(text, @"spectator\.osu\b", "spectator");
        
        await File.WriteAllTextAsync(LazerStorage.ProductionEndpointPath, text, ct);

        // Make lazer trust our hostname
        await ReplaceInFile(
            LazerStorage.TrustedDomainStorePath,
            "if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !uri.Host.EndsWith(@\".ppy.sh\", StringComparison.OrdinalIgnoreCase))",
            $"if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !(uri.Host.EndsWith(@\".ppy.sh\", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(@\".{AppSettings.Domain}\", StringComparison.OrdinalIgnoreCase)))",
            ct
        );
        
        // Change updater url to use our internal one
        await ReplaceInFile(LazerStorage.VelopackUpdaterPath, "github.com", $"api.{AppSettings.Domain}", ct);
        
        //TODO this might be removed, check it once in a while
        await ReplaceInFile(LazerStorage.DrawableAvatarPath, "ppy.sh", AppSettings.Domain, ct);
    }

    private static async Task ReplaceInFile(
        string path,
        string source,
        string target,
        CancellationToken ct
    ) {
        var text = await File.ReadAllTextAsync(path, ct);
        text = text.Replace(source, target);
        await File.WriteAllTextAsync(path, text, ct);
    }
    
    private sealed record GitHubRelease(string TagName, bool Draft, bool Prerelease)
    {
        public static GitHubRelease FromJson(JsonElement e)
        {
            return new GitHubRelease(
                TagName: e.GetProperty("tag_name").GetString() ?? "",
                Draft: e.GetProperty("draft").GetBoolean(),
                Prerelease: e.GetProperty("prerelease").GetBoolean());
        }
    }
}