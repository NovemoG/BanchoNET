using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Handlers.Stable.Controllers.OsuApi;

public partial class OsuController
{
    [HttpGet("/d/{beatmapsetArg}")]
    public async Task<IActionResult> GetOszFile(string beatmapsetArg)
    {
        /*var noVideo = beatmapsetArg[^1] == 'n';
        if (noVideo)
            beatmapsetArg = beatmapsetArg[..^1];*/
        
        var beatmapsetId = int.Parse(beatmapsetArg[..^1]);
        
        var beatmapsetPath = Storage.GetBeatmapsetPath(beatmapsetId);
        if (!System.IO.File.Exists(beatmapsetPath))
        {
            var downloaded = await beatmapDownloader.DownloadBeatmap(beatmapsetId);
            if (!downloaded) return NotFound();
        }
        
        return new PhysicalFileResult(beatmapsetPath, "application/zip");
        
        /*var redirectUrl = $"{AppSettings.OsuDirectDownloadEndpoint}{beatmapsetId}?n={(noVideo ? 0 : 1)}";
        return RedirectPermanent(redirectUrl);*/
    }
}