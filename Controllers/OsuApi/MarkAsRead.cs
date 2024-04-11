using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpGet("/web/osu-markasread.php")]
	public async Task<IActionResult> MarkAsRead(
		[FromQuery(Name = "u")] string username,
		[FromQuery(Name = "h")] string passwordMD5,
		[FromQuery] string? channel)
	{
		if (string.IsNullOrEmpty(channel))
			return Ok();
		
		if (await _bancho.GetPlayerFromLogin(username, passwordMD5) == null)
			return Unauthorized("auth fail");
		
		//TODO

		return Ok();
	}
}