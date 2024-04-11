using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpGet("/web/maps/{mapFilename}")]
	public IActionResult GetUpdatedBeatmap(
		string mapFilename,
		[FromHeader(Name = "host")] string host)
	{
		if (host == "osu.ppy.sh")
			return BadRequest("We only support the -devserver connection method");

		var rawPath = Request.PathBase + Request.Path + Request.QueryString;
		var redirectUrl = $"https://osu.ppy.sh{rawPath}";
        
		return RedirectPermanent(redirectUrl);
	}
}