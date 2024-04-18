using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
    [HttpGet("/d/{mapSetId}")]
    public async Task<IActionResult> GetOszFile(string mapSetId)
    {
        var noVideo = mapSetId[^1] == 'n';
        if (noVideo)
            mapSetId = mapSetId[..^1];

        //TODO if store locally is set to true use a loop like in OsuSearch to find a working mirror
        //     and download the file to the server and then return a file stream result
        
        var downloadEndpoint = AppSettings.OsuDirectDownloadEndpoints[0];
        var redirectUrl = $"{downloadEndpoint}/{mapSetId}?n={(!noVideo ? 1 : 0)}";
        
        return RedirectPermanent(redirectUrl);
    }
}