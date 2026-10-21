using System.Text.Json;
using System.Text.RegularExpressions;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Updater;

[ApiController]
[Route("api/v3/repos")]
[SubdomainAuthorize("api")]
public class UpdaterController(
    IReleasesRepository releases
) : ControllerBase
{
    private static string BaseUrl => $"https://api.{AppSettings.Domain}/api/v3/repos";

    [HttpGet("ppy/osu/releases")]
    [HttpGet("ppy/osu/{stream}/releases")]
    [HttpGet("ppy/osu/{stream}/{baseVersion}/releases")]
    public async Task<ActionResult<List<GithubFeed>>> GetReleases(
        [FromQuery(Name = "per_page")] int perPage = 10,
        int page = 1,
        [FromRoute] string? stream = null,
        [FromRoute] string? baseVersion = null
    ) {
        if (perPage is < 1 or > 100 || page is < 1 or > 100000 || stream is not (null or "lazer" or "tachyon"))
            return BadRequest();

        var published = await PublishedLazerReleases.List(HttpContext.RequestAborted);
        var selected = stream == null ? published : PublishedLazerReleases.ForStream(published, stream);
        var fullOnly = stream == null || !PublishedLazerReleases.CanUseDeltas(
            PublishedLazerReleases.Root,
            selected,
            baseVersion,
            Math.Min(perPage, 10)
        );
        
        var feeds = selected.Select(r => new GithubFeed
        {
            TagName = r.Version,
            Name = r.Version,
            Prerelease = stream != "lazer" && r.Channel == "tachyon",
            PublishedAt = r.PublishedAt,
            Assets = r.Files.Where(f => (!fullOnly || f.Kind != "delta")
                && System.IO.File.Exists(PublishedLazerReleases.FilePath(r, f))).Select(f => Asset(
                f.Name,
                $"{BaseUrl}/{r.Version}/asset/{Uri.EscapeDataString(f.Name)}?fullOnly={fullOnly}",
                f.Kind == "feed"
            )).ToList()
        }).ToList();

        var legacy = stream == null ? await releases.GetReleases(int.MaxValue, 0) : [];

        foreach (var group in legacy.GroupBy(r => r.Version))
        {
            if (published.Any(r => r.Version == group.Key)) continue;

            var full = group.FirstOrDefault(r => r.Type.Equals("Full", StringComparison.OrdinalIgnoreCase));
            if (full == null || !SafeTag(full.Version)) continue;

            var feed = new GithubFeed
            {
                TagName = full.Version,
                Name = full.Version,
                Prerelease = full.Prerelease,
                PublishedAt = full.PublishedAt
            };

            foreach (var platform in new[] { LazerPlatform.Win, LazerPlatform.Linux })
            {
                var platformName = platform == LazerPlatform.Win ? "win" : "linux";
                var feedPath = LazerStorage.GetReleasesPath(full.Version, platform);
                if (!System.IO.File.Exists(feedPath)) continue;

                await using var input = System.IO.File.OpenRead(feedPath);

                var stored = await JsonSerializer.DeserializeAsync<VelopackReleaseFeed>(input);
                if (stored == null) continue;

                feed.Assets.Add(Asset(
                    $"releases.{platformName}.json",
                    $"{BaseUrl}/{full.Version}/releases?platform={platformName}",
                    true
                ));

                foreach (var a in stored.Assets.Where(a => a.Version == full.Version))
                {
                    var type = a.Type.ToLowerInvariant();
                    if (type is not ("full" or "delta")
                        || !System.IO.File.Exists(LazerStorage.GetReleaseFilePath(full.Version, type, platform)))
                    {
                        continue;
                    }

                    feed.Assets.Add(Asset(
                        a.FileName,
                        $"{BaseUrl}/{full.Version}/file/{type}?platform={platformName}"
                    ));
                }
            }

            if (feed.Assets.Count > 0)
                feeds.Add(feed);
        }

        return new JsonResult(feeds
            .OrderByDescending(r => r.PublishedAt)
            .ThenByDescending(r => r.TagName)
            .Skip((page - 1) * perPage)
            .Take(perPage));
    }

    [HttpGet("release-base")]
    public async Task<IActionResult> GetReleaseBase()
    {
        var latest = (await PublishedLazerReleases.List(HttpContext.RequestAborted)).FirstOrDefault();

        return latest == null ? NoContent() : Serve(Path.Combine(PublishedLazerReleases.Root, latest.Version, "manifest.json"), true);
    }

    private static GithubRelease Asset(
        string name,
        string url,
        bool json = false
    ) => new()
    {
        Name = name,
        Url = url,
        BrowserDownloadUrl = url,
        ContentType = json ? "application/json" : "application/octet-stream"
    };

    private static bool SafeTag(
        string tag
    ) => Regex.IsMatch(tag, @"^[a-zA-Z0-9][a-zA-Z0-9.\-]{0,63}$");

    [HttpGet("{tagName}/{type}/{package?}")]
    public async Task<IActionResult> GetReleaseFile(
        string tagName,
        string type,
        string? package = null,
        [FromQuery] LazerPlatform platform = LazerPlatform.Win,
        bool fullOnly = false
    ) {
        if (!SafeTag(tagName) || !Enum.IsDefined(platform)) return BadRequest();

        var release = (await PublishedLazerReleases.List()).FirstOrDefault(r => r.Version == tagName);
        if (release != null)
        {
            var platformName = platform == LazerPlatform.Win ? "win" : "linux";
            var file = type == "asset"
                ? release.Files.FirstOrDefault(f => f.Name == package)
                : release.Files.FirstOrDefault(f => f.Platform == platformName
                    && f.Kind == (type switch
                    {
                        "releases" => "feed",
                        "file" => package,
                        _ => null
                    }));

            if (file == null) return NotFound();
            if (file.Kind != "feed") return Serve(PublishedLazerReleases.FilePath(release, file));

            var feed = await release.ReadFeed(
                Path.Combine(PublishedLazerReleases.Root, release.Version),
                file.Platform,
                fullOnly,
                HttpContext.RequestAborted
            );

            return new JsonResult(feed, new JsonSerializerOptions { PropertyNamingPolicy = null });
        }

        if (type == "releases")
            return Serve(LazerStorage.GetReleasesPath(tagName, platform), true);

        if (type == "file" && package is "full" or "delta")
            return Serve(LazerStorage.GetReleaseFilePath(tagName, package, platform));

        return BadRequest();
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download(
        bool tachyon = false,
        [FromQuery] LazerPlatform platform = LazerPlatform.Win,
        bool installer = false
    ) {
        if (!Enum.IsDefined(platform) || (installer && platform != LazerPlatform.Win)) return BadRequest();

        var latest = (await PublishedLazerReleases.List()).FirstOrDefault(r => tachyon || r.Channel == "lazer");
        if (latest != null)
        {
            return Serve(PublishedLazerReleases.FilePath(
                latest,
                latest.Single(platform == LazerPlatform.Win ? "win" : "linux", installer ? "setup" : "portable")
            ));
        }

        return installer ? NotFound() : Serve(LazerStorage.GetLazerPortablePath(tachyon, platform));
    }

    private IActionResult Serve(
        string path,
        bool json = false
    ) => !System.IO.File.Exists(path) ? NotFound() : PhysicalFile(
        path,
        json ? "application/json" : "application/octet-stream",
        fileDownloadName: Path.GetFileName(path),
        enableRangeProcessing: true
    );
}