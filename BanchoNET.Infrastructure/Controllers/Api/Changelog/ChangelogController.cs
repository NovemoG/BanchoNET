using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Changelog;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Changelog;

[ApiController]
[Route("api/v2/changelog")]
[SubdomainAuthorize("osu")]
public class ChangelogController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ChangelogIndexResponse>> GetChangelog()
    {
        var releases = await PublishedLazerReleases.List(HttpContext.RequestAborted);

        return new JsonResult(LazerChangelog.Index(releases), SnakeCaseNamingPolicy.Options);
    }

    [HttpGet("{stream}/{version}")]
    public async Task<ActionResult<ChangelogBuild>> GetBuild(
        string stream,
        string version
    ) {
        var releases = await PublishedLazerReleases.List(HttpContext.RequestAborted);
        var build = LazerChangelog.Single(releases, stream, version);

        return build == null ? NotFound() : new JsonResult(build, SnakeCaseNamingPolicy.Options);
    }
}