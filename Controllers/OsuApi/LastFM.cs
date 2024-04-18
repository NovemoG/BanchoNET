using BanchoNET.Objects.Players;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpGet("lastfm.php")]
	public async Task<IActionResult> LastFM(
		[FromQuery(Name = "b")]string beatmapId, 
		[FromQuery(Name = "c")]string beatmapNameBase64,
		[FromQuery(Name = "action")]string action,
		[FromQuery(Name = "us")]string username,
		[FromQuery(Name = "ha")]string passwordMD5)
	{
		var player = await _bancho.GetPlayerFromLogin(username, passwordMD5);
		if (player == null)
			return Unauthorized("auth fail");
		
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