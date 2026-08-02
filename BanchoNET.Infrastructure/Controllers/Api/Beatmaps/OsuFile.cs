using BanchoNET.Core.Attributes;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpGet("{beatmapId:int}/osu")]
    [AllowClientCredentials]
    public async Task<ActionResult<string>> GetOsuFile(
        int beatmapId
    ) {
        var beatmap = await Beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null) return NotFound();
        
        if (!await Beatmaps.EnsureLocalBeatmapFile(beatmap))
            return NotFound();
        
        var filePath = Storage.GetBeatmapPath(beatmapId);
        var fileContents = await System.IO.File.ReadAllBytesAsync(filePath);
        
        return File(fileContents, "application/octet-stream");
    }
}