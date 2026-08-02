using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[ApiController]
[Route("api/v3/repos")]
[SubdomainAuthorize("api")]
public class UpdaterController(IReleasesRepository releases) : ControllerBase
{
    [HttpGet("ppy/osu/releases")]
    public async Task<ActionResult<List<GithubFeed>>> GetReleases(
        [FromQuery(Name = "per_page")] int perPage = 10,
        int page = 1
    ) {
        var dbReleases = await releases.GetReleases(perPage * 2, (page - 1) * perPage * 2);
        if (dbReleases.Count == 0) return NotFound();

        var githubFeed = new List<GithubFeed>();
        for (var i = 0; i < dbReleases.Count; i++)
        {
            var full = dbReleases[i];
            var baseUrl = $"https://api.{AppSettings.Domain}/api/v3/repos";

            var feed = new GithubFeed
            {
                TagName = full.Version, Name = full.Version, Prerelease = full.Prerelease,
                PublishedAt = full.PublishedAt,
                Assets =
                [
                    new GithubRelease
                    {
                        Url = baseUrl,
                        Name = "releases.win.json",
                        ContentType = "application/json",
                        BrowserDownloadUrl = $"{baseUrl}/{full.Version}/releases"
                    },
                    new GithubRelease
                    {
                        Url = baseUrl,
                        Name = "releases.linux.json",
                        ContentType = "application/json",
                        BrowserDownloadUrl = $"{baseUrl}/{full.Version}/releases?platform=linux"
                    }
                ]
            };
            
            githubFeed.Add(feed);
            
            if (dbReleases.Count - 1 != i)
            {
                var delta = dbReleases[++i];
                
                feed.Assets.Add(new GithubRelease
                {
                    Url = baseUrl,
                    Name = $"{AppSettings.LazerName}-{delta.Version}-delta.nupkg",
                    ContentType = "application/octet-stream",
                    BrowserDownloadUrl = $"{baseUrl}/{delta.Version}/file/delta",
                });
                
                feed.Assets.Add(new GithubRelease
                {
                    Url = baseUrl,
                    Name = $"{AppSettings.LazerName}-{delta.Version}-linux-delta.nupkg",
                    ContentType = "application/octet-stream",
                    BrowserDownloadUrl = $"{baseUrl}/{delta.Version}/file/delta?platform=linux",
                });
            }
            
            feed.Assets.Add(new GithubRelease
            {
                Url = baseUrl,
                Name = $"{AppSettings.LazerName}-{full.Version}-full.nupkg",
                ContentType = "application/octet-stream",
                BrowserDownloadUrl = $"{baseUrl}/{full.Version}/file/full",
            });
            
            feed.Assets.Add(new GithubRelease
            {
                Url = baseUrl,
                Name = $"{AppSettings.LazerName}-{full.Version}-linux-full.nupkg",
                ContentType = "application/octet-stream",
                BrowserDownloadUrl = $"{baseUrl}/{full.Version}/file/full?platform=linux",
            });
        }
        
        return new JsonResult(githubFeed);
    }

    [HttpGet("{tagName}/{type}/{package?}")]
    public async Task<IActionResult> GetReleaseFile(
        string tagName,
        string type,
        string? package = null,
        [FromQuery] LazerPlatform platform = LazerPlatform.Win
    ) {
        if (type.Equals("releases", StringComparison.OrdinalIgnoreCase))
        {
            var filePath = LazerStorage.GetReleasesPath(tagName, platform);
            if (!System.IO.File.Exists(filePath))
                return NotFound();
            
            var text = await System.IO.File.ReadAllTextAsync(filePath);
            return Content(text, "application/json");
        }
        
        if (type.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(package))
                return BadRequest();

            var filePath = LazerStorage.GetReleaseFilePath(tagName, package, platform);
            if (!System.IO.File.Exists(filePath))
                return NotFound();
            
            return PhysicalFile(
                filePath,
                "application/octet-stream",
                fileDownloadName: Path.GetFileName(filePath),
                enableRangeProcessing: true
            );
        }

        return BadRequest();
    }

    [HttpGet("download")]
    public IActionResult Download(
        bool tachyon = false,
        [FromQuery] LazerPlatform platform = LazerPlatform.Win
    ) {
        var filePath = LazerStorage.GetLazerPortablePath(tachyon, platform);
        if (!System.IO.File.Exists(filePath))
            return NotFound();
        
        return PhysicalFile(
            filePath,
            "application/octet-stream",
            fileDownloadName: Path.GetFileName(filePath),
            enableRangeProcessing: true
        );
    }
}