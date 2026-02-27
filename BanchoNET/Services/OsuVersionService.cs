using System.Diagnostics;
using System.Text.Json;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Hangfire;

namespace BanchoNET.Services;

public class OsuVersionService(ILogger logger) : IOsuVersionService
{
    private readonly HttpClient _client = new();
    private const string OsuApiV2ChangelogUrl = "https://osu.ppy.sh/api/v2/changelog";
    private readonly Dictionary<string, OsuVersion> _streams = new()
    {
        { "stable40", new OsuVersion() },
        { "cuttingedge", new OsuVersion() },
    };

    public async Task Init()
    {
        await FetchOsuVersion();
        
        RecurringJob.AddOrUpdate(
            "fetchOsuVersion",
            () => FetchOsuVersion(),
            $"0 */{AppSettings.VersionFetchHoursInterval} * * *"); // every x hours
    }
    
    public async Task FetchOsuVersion()
    {
        logger.LogInfo("Fetching osu versions...", caller: nameof(OsuVersionService));
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        foreach (var clientStream in _streams)
        {
            var response = await _client.GetAsync($"{OsuApiV2ChangelogUrl}?stream={clientStream.Key}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            
            if (!content.IsValidResponse()) continue;
            
            var changelog = JsonSerializer.Deserialize<ClientBuildVersions>(content);
            foreach (var build in changelog!.Builds) //Can't be null because we check for valid content length
            {
                _streams[clientStream.Key] = OsuVersion.Parse(clientStream.Key, build.Version);
                
                if (build.ChangelogEntries.Any(entry => entry.Major)) break;
            }
        }

        var newerDates = false;
        var filePath = Storage.GetMajorOsuVersionFilePath();
        if (!File.Exists(filePath))
        {
            await WriteToFile();
            
            stopwatch.Stop();
            logger.LogInfo($"Fetched osu versions in {stopwatch.Elapsed}", caller: nameof(OsuVersionService));
            return;
        }
        
        var versionsText = (await File.ReadAllTextAsync(filePath)).Trim().Split("\n");
        for (int i = 0; i < _streams.Count; i++)
        {
            var stream = versionsText[i].Split("=");

            if (_streams.TryGetValue(stream[0], out var version))
            {
                var cachedVersion = OsuVersion.Parse(stream[0], stream[1]);
                
                if (version < cachedVersion)
                {
                    _streams[stream[0]] = cachedVersion;
                    continue;
                }
                newerDates = true;
            }
            else newerDates = true;
        }
        
        if (newerDates)
        {
            logger.LogInfo("Caching updated versions", caller: nameof(OsuVersionService));
            await WriteToFile();
        }
        
        stopwatch.Stop();
        logger.LogInfo($"Fetched osu versions in {stopwatch.Elapsed}", caller: nameof(OsuVersionService));
    }
    
    public OsuVersion GetLatestVersion(string stream)
    {
        return _streams[stream];
    }

    private async Task WriteToFile()
    {
        await File.WriteAllLinesAsync(Storage.GetMajorOsuVersionFilePath(), _streams.Select(s => $"{s.Key}={s.Value}"));
    }
}