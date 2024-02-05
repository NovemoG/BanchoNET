using BanchoNET.Models;
using BanchoNET.Packets;
using BanchoNET.Services;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BanchoNET.Controllers.Cho;

[ApiController]
public partial class ChoController : ControllerBase
{
	private readonly BanchoHandler _bancho;
	private readonly ServerConfig _config;
	private readonly HttpClient _httpClient;
	
	public ChoController(BanchoHandler bancho, IOptions<ServerConfig> serverConstants, HttpClient httpClient)
	{
		_bancho = bancho;
		_config = serverConstants.Value;
		_httpClient = httpClient;
	}
	
	[HttpPost("/")]
	public async Task<IActionResult> BanchoHandler()
	{
		var osuToken = Request.Headers["osu-token"];
		if (string.IsNullOrEmpty(osuToken))
		{
			return await Login();
		}

		var player = _bancho.GetPlayerSession(token: new Guid(osuToken!));
		if (player == null)
		{
			var restartData = new ServerPackets();
			
			restartData.Notification("Server has restarted.");
			restartData.RestartServer(0);
			
			return restartData.GetContentResult();
		}
		
		using var clientData = await new ClientPackets().CopyStream(Request.Body);
		clientData.ReadPackets(player);
		
		player.LastActivityTime = DateTime.Now;

		return new FileContentResult(player.Dequeue(), "application/octet-stream; charset=UTF-8");
	}

	[HttpGet("/")]
	public async Task<ContentResult> BanchoHttpHandler()
	{
		Console.WriteLine("BanchoHTTP");
		
		return new ContentResult
		{
			ContentType = "text/html",
			StatusCode = 200,
			Content = "<!DOCTYPE html><body style=\"font-family: monospace; white-space: pre-wrap;\">Test</a></body></html>"
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