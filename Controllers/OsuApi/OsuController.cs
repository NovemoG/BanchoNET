using BanchoNET.Objects;
using BanchoNET.Services;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[ApiController]
public partial class OsuController : ControllerBase
{
	private readonly BanchoHandler _bancho;
	private readonly GeolocService _geoloc;
	private readonly BanchoSession _session;

	public OsuController(BanchoHandler bancho, GeolocService geoloc)
	{
		_bancho = bancho;
		_geoloc = geoloc;
		_session = BanchoSession.Instance;
	}
	
	[HttpPost("/web/osu-error.php")]
	public async Task<IActionResult> OsuError()
	{
		Console.WriteLine("OsuError");
		return Ok("");
	}

	[HttpGet("/web/bancho_connect.php")]
	public async Task<IActionResult> BanchoConnect()
	{
		Console.WriteLine("BanchoConnect");
		return Ok();
	}

	[HttpGet("/web/check-updates.php")]
	public async Task<IActionResult> CheckUpdates()
	{
		Console.WriteLine("CheckUpdates");
		return Ok();
	}

	[HttpGet("/web/lastfm.php")]
	public async Task<IActionResult> LastFM(
		[FromQuery(Name = "b")]string beatmapId, 
		[FromQuery(Name = "c")]string beatmapNameBase64,
		[FromQuery(Name = "action")]string action,
		[FromQuery(Name = "us")]string username,
		[FromQuery(Name = "ha")]string passwordHash)
	{
		if (beatmapId[0] != 'a') return Ok("-3");

		var flags = (LastFmfLags)int.Parse(beatmapId[1..]);

		if ((flags & (LastFmfLags.HqAssembly | LastFmfLags.HqFile)) != 0)
		{
			//TODO restrict player
			
			//TODO logout player

			return Ok("-3");
		}

		if ((flags & LastFmfLags.RegistryEdits) != 0)
		{
			//TODO
		}
		
		return Ok();
	}
}