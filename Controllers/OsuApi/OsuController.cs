using BanchoNET.Objects;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[Route("web/")]
[ApiController]
public partial class OsuController : ControllerBase
{
	[HttpPost("osu-error.php")]
	public async Task OsuError()
	{
		Console.WriteLine("OsuError");
	}

	[HttpGet("bancho_connect.php")]
	public async Task<IActionResult> BanchoConnect()
	{
		Response.ApplyHeaders();
		
		Console.WriteLine("BanchoConnect");
		return Ok();
	}

	[HttpGet("check-updates.php")]
	public async Task CheckUpdates()
	{
		
	}

	[HttpGet("lastfm.php")]
	public async Task<IActionResult> LastFM(
		[FromQuery(Name = "b")]string beatmapId, 
		[FromQuery(Name = "c")]string beatmapNameBase64,
		[FromQuery(Name = "action")]string action,
		[FromQuery(Name = "us")]string username,
		[FromQuery(Name = "ha")]string passwordHash)
	{
		Response.ApplyHeaders();
		
		if (beatmapId[0] != 'a')
		{
			return Ok("-3");
		}

		var flags = (LastFMFLags)int.Parse(beatmapId[1..]);

		if ((flags & (LastFMFLags.HQ_ASSEMBLY | LastFMFLags.HQ_FILE)) != 0)
		{
			//TODO restrict player
			
			//TODO logout player

			return Ok("-3");
		}

		if ((flags & LastFMFLags.REGISTRY_EDITS) != 0)
		{
			//TODO
		}
		
		return Ok();
	}
}