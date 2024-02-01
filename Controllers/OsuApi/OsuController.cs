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
	public async Task BanchoConnect()
	{
		Console.WriteLine("BanchoConnect");
	}

	[HttpGet("check-updates.php")]
	public async Task CheckUpdates()
	{
		
	}
}