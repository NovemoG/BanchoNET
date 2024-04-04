using BanchoNET.Models;
using BanchoNET.Objects;
using Hangfire;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BanchoNET.Services;

public class OsuVersionService
{
    private readonly HttpClient _client = new();
    private const string OsuApiV2ChangelogUrl = "https://osu.ppy.sh/api/v2/changelog";
    private readonly Dictionary<string, OsuVersion> _streams = new()
    {
        {"stable40", new OsuVersion()},
        {"beta40", new OsuVersion()},
        {"cuttingedge", new OsuVersion()},
    };

    public OsuVersionService(IOptions<ServerConfig> config)
    {
        RecurringJob.AddOrUpdate(
            "fetchOsuVersion",
            () => FetchOsuVersion(),
            Cron.Hourly(config.Value.VersionFetchHoursDelay));
    }

    public async Task FetchOsuVersion()
    {
        Console.WriteLine($"[{GetType().Name}] fetching osu versions execution date: {DateTime.Now}");
        foreach (var clientStream in _streams)
        {
            var response = await _client.GetAsync($"{OsuApiV2ChangelogUrl}?stream={clientStream.Key}");
            response.EnsureSuccessStatusCode();

            var changelog = JsonConvert.DeserializeObject<ClientBuildVersions>(await response.Content.ReadAsStringAsync());
            foreach (var build in changelog.Builds)
            {
                _streams[clientStream.Key] = new OsuVersion
                {
                    Date = DateTime.ParseExact(build.Version[..8], "yyyyMMdd", null),
                    Revision = build.Version.Contains('.') ? int.Parse(build.Version[9..]) : 0,
                    Stream = clientStream.Key
                };

                if (build.ChangelogEntries.Any(entry => entry.Major)) break;
            }
        }
    }
    
    public OsuVersion GetLatestVersion(string stream)
    {
        return _streams[stream];
    }
}