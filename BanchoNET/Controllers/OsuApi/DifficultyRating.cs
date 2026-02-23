using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
    [HttpPost("/difficulty-rating")]
    public IActionResult DifficultyRating()
    {
        var rawPath = Request.PathBase + Request.Path + Request.QueryString;
        var redirectUrl = $"https://osu.ppy.sh{rawPath}";
        
        return RedirectPermanent(redirectUrl);
    }
}