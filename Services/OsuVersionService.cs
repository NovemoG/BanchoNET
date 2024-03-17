using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using Hangfire;
using Newtonsoft.Json;

namespace BanchoNET.Services;

public class OsuVersionService
{
    private readonly HttpClient _client = new HttpClient();
    private const string OsuApiV2ChangelogUrl = "https://osu.ppy.sh/api/v2/changelog";
    
    
    public readonly Dictionary<string, OsuVersion> Streams = new()
    {
        {"stable40", new()},
        {"beta40", new()},
        {"cuttingedge", new()},
        
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
        Console.WriteLine($"[{GetType().Name}] fetching osu versions execution date: {DateTime.Now} ");
        foreach (var clientStream in Streams)
        {
            var response = await _client.GetAsync($"{OsuApiV2ChangelogUrl}?stream={clientStream.Key}");
            response.EnsureSuccessStatusCode();
            var changelog = JsonConvert.DeserializeObject<ClientBuildVersions>(await response.Content.ReadAsStringAsync());
            foreach (var build in changelog.builds)
            {
                Streams[clientStream.Key] = new OsuVersion
                {
                    Date = DateTime.ParseExact(build.version[..8], "yyyyMMdd", null),
                    Revision = build.version.Contains('.') ? int.Parse(build.version[9..]) : 0,
                    Stream = clientStream.Key
                };

                if (build.changelog_entries.Any(entry => entry.major)) break;
            }
        }
    }
}