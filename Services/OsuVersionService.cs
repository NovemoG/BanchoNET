using BanchoNET.Models;
using BanchoNET.Objects;
using Hangfire;
using Newtonsoft.Json;

namespace BanchoNET.Services;

public class OsuVersionService
{
    private readonly HttpClient _client = new();
    private const string OsuApiV2ChangelogUrl = "https://osu.ppy.sh/api/v2/changelog";
    
    public readonly Dictionary<string, OsuVersion> Streams = new()
    {
        {"stable40", new OsuVersion()},
        {"beta40", new OsuVersion()},
        {"cuttingedge", new OsuVersion()},
    };
    
    public OsuVersionService()
    {
        RecurringJob.AddOrUpdate(
            "fetchOsuVersion",
            () => FetchOsuVersion(),
            Cron.Hourly);
    }

    public async Task FetchOsuVersion()
    {
        Console.WriteLine($"[{GetType().Name}] fetching osu versions execution date: {DateTime.Now}");
        foreach (var clientStream in Streams)
        {
            var response = await _client.GetAsync($"{OsuApiV2ChangelogUrl}?stream={clientStream.Key}");
            response.EnsureSuccessStatusCode();
            
            var changelog = JsonConvert.DeserializeObject<ClientBuildVersions>(await response.Content.ReadAsStringAsync());
            foreach (var build in changelog.Builds)
            {
                Streams[clientStream.Key] = new OsuVersion
                {
                    Date = DateTime.ParseExact(build.Version[..8], "yyyyMMdd", null),
                    Revision = build.Version.Contains('.') ? int.Parse(build.Version[9..]) : 0,
                    Stream = clientStream.Key
                };

                if (build.ChangelogEntries.Any(entry => entry.Major)) break;
            }
        }
    }
}