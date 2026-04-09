using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

[ApiController]
[Route("api/v3/repos")]
[SubdomainAuthorize("osu")]
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
            var feed = new GithubFeed
            {
                TagName = full.Version,
                Name = full.Version,
                Prerelease = full.Prerelease,
                PublishedAt = full.PublishedAt,
                Assets = [
                    new GithubRelease
                    {
                        Url = $"https://osu.{AppSettings.Domain}/api/v3/repos",
                        Name = "releases.win.json",
                        ContentType = "application/json",
                        BrowserDownloadUrl = $"https://osu.{AppSettings.Domain}/api/v3/repos/{full.Version}/releases/releases.win.json"
                    },
                    new GithubRelease
                    {
                        Url = $"https://osu.{AppSettings.Domain}/api/v3/repos",
                        Name = $"{full.Version}-full.nupkg",
                        ContentType = "application/octet-stream",
                        BrowserDownloadUrl = $"https://osu.{AppSettings.Domain}/api/v3/repos/{full.Version}/file/{full.Version}-full.nupkg",
                    }
                ]
            };
            githubFeed.Add(feed);
            
            if (dbReleases.Count == i) break;

            var delta = dbReleases[++i];
            feed.Assets.Add(new GithubRelease
            {
                Url = $"https://osu.{AppSettings.Domain}/api/v3/repos",
                Name = $"{delta.Version}-delta.nupkg",
                ContentType = "application/octet-stream",
                BrowserDownloadUrl = $"https://osu.{AppSettings.Domain}/api/v3/repos/{delta.Version}/file/{delta.Version}-delta.nupkg",
            });
        }
        
        return new JsonResult(githubFeed);
    }

    [HttpGet("{tagName}/{type}/{fileName}")]
    public async Task<IActionResult> GetReleaseFile(
        string tagName,
        string type,
        string fileName
    ) {
        if (!System.IO.File.Exists(LazerStorage.GetReleasesPath(tagName)))
            return NotFound();

        if (type.Equals("releases", StringComparison.OrdinalIgnoreCase))
        {
            var text = await System.IO.File.ReadAllTextAsync(LazerStorage.GetReleasesPath(tagName));
            return new JsonResult(text);
        }
        
        if (type.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            var filePath = LazerStorage.GetReleaseFilePath(tagName, fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound();
            
            return PhysicalFile(
                filePath,
                "application/octet-stream",
                fileDownloadName: fileName,
                enableRangeProcessing: true
            );
        }

        return BadRequest();
    }
}