using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services;

public class OsuVersionService(
    ILogger logger,
    IHttpClientFactory httpClientFactory
) : BackgroundService, IOsuVersionService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(AppSettings.VersionFetchHoursInterval);
    private const string OsuApiV2ChangelogUrl = "https://osu.ppy.sh/api/v2/changelog";
    private readonly ConcurrentDictionary<string, OsuVersion> _streams = new(new Dictionary<string, OsuVersion>
    {
        { "stable40", new OsuVersion() },
        { "cuttingedge", new OsuVersion() },
    });

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        try
        {
            await FetchOsuVersion(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ignore during startup
        }
        catch (Exception ex)
        {
            logger.LogError("Error while fetching osu versions.", ex, caller: nameof(OsuVersionService));
        }
        
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await FetchOsuVersion(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError("Error while fetching osu versions.", ex, caller: nameof(OsuVersionService));
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
    }
    
    public async Task FetchOsuVersion(CancellationToken ct)
    {
        logger.LogInfo("Fetching osu versions...", caller: nameof(OsuVersionService));
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var client = httpClientFactory.CreateClient(nameof(OsuVersionService));
        
        foreach (var clientStream in _streams)
        {
            var response = await client.GetAsync($"{OsuApiV2ChangelogUrl}?stream={clientStream.Key}", ct);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(ct);
            
            if (!content.IsValidResponse()) continue;
            
            var changelog = JsonSerializer.Deserialize<ClientBuildVersions>(content);
            foreach (var build in changelog!.Builds) //Can't be null because we check for valid content length
            {
                var version = OsuVersion.Parse(clientStream.Key, build.Version);
                _streams.AddOrUpdate(clientStream.Key, version, (_, _) => version);
                
                if (build.ChangelogEntries.Any(entry => entry.Major))
                {
                    logger.LogInfo($"[{clientStream.Key}] Current newest major version {version}");
                    break;
                }
            }
        }

        var newerDates = false;
        var filePath = Storage.GetMajorOsuVersionFilePath();
        if (!File.Exists(filePath))
        {
            await WriteToFile(ct);
            
            stopwatch.Stop();
            logger.LogInfo($"Fetched osu versions in {stopwatch.Elapsed}", caller: nameof(OsuVersionService));
            return;
        }
        
        var versionsText = (await File.ReadAllTextAsync(filePath, ct)).Trim().Split("\n");
        for (int i = 0; i < _streams.Count; i++)
        {
            var stream = versionsText[i].Split("=");

            if (_streams.TryGetValue(stream[0], out var version))
            {
                var cachedVersion = OsuVersion.Parse(stream[0], stream[1]);
                
                if (version < cachedVersion)
                {
                    _streams.AddOrUpdate(stream[0], cachedVersion, (_, _) => cachedVersion);
                    continue;
                }
                newerDates = true;
            }
            else newerDates = true;
        }
        
        if (newerDates)
        {
            logger.LogInfo("Caching updated versions", caller: nameof(OsuVersionService));
            await WriteToFile(ct);
        }
        
        stopwatch.Stop();
        logger.LogInfo($"Fetched osu versions in {stopwatch.Elapsed}", caller: nameof(OsuVersionService));
    }
    
    public OsuVersion GetLatestVersion(string stream)
    {
        return _streams[stream];
    }

    private async Task WriteToFile(CancellationToken ct)
    {
        await File.WriteAllLinesAsync(Storage.GetMajorOsuVersionFilePath(), _streams.Select(s => $"{s.Key}={s.Value}"), ct);
    }
}