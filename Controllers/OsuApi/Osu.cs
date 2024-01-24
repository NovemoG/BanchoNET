using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.OsuApi;

[Route("web/")]
[ApiController]
public partial class Osu : ControllerBase
{
	[HttpPost("osu-error.php")]
	public async Task<HttpResponse> OsuError()
	{
		
	}
}