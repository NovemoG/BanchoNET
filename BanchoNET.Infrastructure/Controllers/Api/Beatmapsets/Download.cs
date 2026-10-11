using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmapsets;

public partial class BeatmapsetsController
{
    [HttpGet("{beatmapsetId:int}/download")]
    public async Task<IActionResult> DownloadBeatmapset(
        int beatmapsetId,
        [FromQuery] int noVideo
    ) {
        if (beatmapsetId < 1)
            return BadRequest();

        if (!await beatmapDownloader.EnsureBeatmapset(beatmapsetId, HttpContext.RequestAborted))
        {
            Response.Headers.RetryAfter = "30";
            return StatusCode(503);
        }

        var beatmapsetPath = Storage.GetBeatmapsetPath(beatmapsetId);
        return new PhysicalFileResult(beatmapsetPath, "application/zip");
    }
}