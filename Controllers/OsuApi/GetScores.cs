using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

public partial class OsuController
{
	[HttpGet("/web/osu-osz2-getscores.php")]
	public async Task<IActionResult> GetScores()
	{
		return Ok();
	}
}