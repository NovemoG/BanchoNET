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
        var beatmapsetPath = Storage.GetBeatmapsetPath(beatmapsetId);
        if (!System.IO.File.Exists(beatmapsetPath))
        {
            var downloaded = await beatmapDownloader.DownloadBeatmap(beatmapsetId);
            if (!downloaded) return NotFound();
        }
        
        return new PhysicalFileResult(beatmapsetPath, "application/zip");
    }
}