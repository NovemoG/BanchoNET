using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Handlers.Stable.Controllers.OsuApi;

public partial class OsuController
{
    [HttpGet("/d/{beatmapsetArg}")]
    public async Task<IActionResult> GetOszFile(string beatmapsetArg)
    {
        if (beatmapsetArg.EndsWith('n'))
            beatmapsetArg = beatmapsetArg[..^1];

        if (!int.TryParse(beatmapsetArg, out var beatmapsetId) || beatmapsetId < 1)
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