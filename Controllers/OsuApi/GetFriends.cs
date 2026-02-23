using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpGet("osu-getfriends.php")]
	public async Task<IActionResult> BanchoHttpHandler(
		[FromQuery(Name = "u")] string username,
		[FromQuery(Name = "h")] string passwordMD5)
	{
		var player = await players.GetPlayerFromLogin(username, passwordMD5);
		if (player == null)
			return Unauthorized("auth fail");
		
		if (player.Friends.Count == 0)
			await players.GetPlayerRelationships(player);
		
		return Responses.BytesContentResult(string.Join("\n", player.Friends));
	}
}