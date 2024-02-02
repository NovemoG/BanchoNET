using System.Buffers;
using System.Net;
using BanchoNET.Packets.Server;
using BanchoNET.Services;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace BanchoNET.Controllers.Cho;

[ApiController]
public partial class ChoController(BanchoHandler bancho) : ControllerBase
{
	[HttpPost("/")]
	public async Task<IActionResult> BanchoHandler()
	{
		Console.WriteLine("BanchoHandler Post");
		
		Response.ApplyHeaders();

		var osuToken = Request.Headers["cho-token"];
		if (string.IsNullOrEmpty(osuToken))
		{
			return await Login();
		}

		/*Response.Headers["cho-token"] = "incorrect-password";

		using var responseData = new ServerPacket();
		responseData.Notification("Incorrect password");
		responseData.UserId(-1);

		return new FileContentResult(await responseData.GetContent(), "application/octet-stream; charset=UTF-8");*/
		
		//TODO get player from session using token

		return Ok();
	}

	[HttpGet("/")]
	public async Task<ContentResult> BanchoHttpHandler()
	{
		Console.WriteLine("BanchoHTTP");
		
		return new ContentResult
		{
			ContentType = "text/html",
			StatusCode = 200,
			Content =
				$"<!DOCTYPE html><body style=\"font-family: monospace; white-space: pre-wrap;\">Running bancho.py v{19} <a href=\"online\">{1} online players</a><a href=\"matches\">{0} matches</a><b>packets handled ({0})</b>{0}<a href=\"https://github.com/osuAkatsuki/bancho.py\">Source code</a></body></html>"
		};
	}

	/*[HttpGet("matches")]
	public async Task<HttpResponse> ViewOnlineUsers()
	{
		
	}

	[HttpGet("online")]
	public async Task<HttpResponse> ViewMatches()
	{
		
	}*/
}