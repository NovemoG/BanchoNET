using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpGet("osu-markasread.php")]
	public async Task<IActionResult> MarkAsRead(
		[FromQuery(Name = "u")] string username,
		[FromQuery(Name = "h")] string passwordMD5,
		[FromQuery] string? channel)
	{
		/*channel = Request.GetDisplayUrl().Split("=")[2];*/
		
		if (string.IsNullOrEmpty(channel))
			return Ok();
		
		if (await players.GetPlayerFromLogin(username, passwordMD5) == null)
			return Unauthorized("auth fail");
		
		//TODO

		return Ok();
	}
}