using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpGet("~/api/v2/beatmapsets/{beatmapsetId:int}/download")]
    public async Task<IActionResult> DownloadBeatmapset(
        int beatmapsetId,
        [FromQuery] int noVideo
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();

        var beatmapsetPath = Storage.GetBeatmapsetPath(beatmapsetId);
        if (!System.IO.File.Exists(beatmapsetPath))
        {
            var downloaded = await beatmapDownloader.DownloadBeatmap(beatmapsetId);
            if (!downloaded) return NotFound();
        }
        
        return new PhysicalFileResult(beatmapsetPath, "application/zip");
    }
}