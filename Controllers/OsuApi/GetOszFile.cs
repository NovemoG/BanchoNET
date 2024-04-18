using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
    [HttpGet("/d/{mapSetId}")]
    public IActionResult GetOszFile(string mapSetId)
    {
        var noVideo = mapSetId[^1] == 'n';
        if (noVideo)
            mapSetId = mapSetId[..^1];

        var redirectUrl = $"{AppSettings.OsuDirectSearchEndpoints}/{mapSetId}?n={(!noVideo ? 1 : 0)}";
        return RedirectPermanent(redirectUrl);
    }
}