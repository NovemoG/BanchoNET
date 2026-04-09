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
            var feed = new GithubFeed
            {
                TagName = full.Version, Name = full.Version, Prerelease = full.Prerelease,
                PublishedAt = full.PublishedAt,
                Assets = [
                    new GithubRelease
                    {
                        Url = $"https://api.{AppSettings.Domain}/api/v3/repos",
                        Name = "releases.win.json",
                        ContentType = "application/json",
                        BrowserDownloadUrl = $"https://api.{AppSettings.Domain}/api/v3/repos/{full.Version}/releases/releases.{full.Version}.json"
                    }
                ]
            };
            
            githubFeed.Add(feed);
            
            if (dbReleases.Count - 1 != i)
            {
                var delta = dbReleases[++i];
                feed.Assets.Add(new GithubRelease
                {
                    Url = $"https://api.{AppSettings.Domain}/api/v3/repos",
                    Name = $"{AppSettings.LazerName}-{delta.Version}-delta.nupkg",
                    ContentType = "application/octet-stream",
                    BrowserDownloadUrl = $"https://api.{AppSettings.Domain}/api/v3/repos/{delta.Version}/file/{AppSettings.LazerName}-{delta.Version}-delta.nupkg",
                });
            }
            
            feed.Assets.Add(new GithubRelease
            {
                Url = $"https://api.{AppSettings.Domain}/api/v3/repos",
                Name = $"{AppSettings.LazerName}-{full.Version}-full.nupkg",
                ContentType = "application/octet-stream",
                BrowserDownloadUrl = $"https://api.{AppSettings.Domain}/api/v3/repos/{full.Version}/file/{AppSettings.LazerName}-{full.Version}-full.nupkg",
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
            var text = await System.IO.File.ReadAllTextAsync(LazerStorage.GetReleaseFilePath(fileName));
            return Content(text, "application/json");
        }
        
        if (type.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            var filePath = LazerStorage.GetReleaseFilePath(fileName);
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