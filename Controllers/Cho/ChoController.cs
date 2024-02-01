using BanchoNET.Services;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Cho;

[ApiController]
public partial class ChoController(BanchoHandler bancho) : ControllerBase
{
	[HttpPost("/")]
	public async Task<ActionResult> BanchoHandler()
	{
		Console.WriteLine("BanchoHandler Post");
		
		Response.ApplyHeaders();

		var osuToken = Request.Headers["cho-token"];
		if (string.IsNullOrEmpty(osuToken))
		{
			return await Login();
		}

		//TODO get player from session using token

		return Ok();
	}

	/*[HttpGet("/")]
	public async Task<ActionResult> BanchoHttpHandler()
	{
		
	}

	[HttpGet("matches")]
	public async Task<HttpResponse> ViewOnlineUsers()
	{
		
	}

	[HttpGet("online")]
	public async Task<HttpResponse> ViewMatches()
	{
		
	}*/
}