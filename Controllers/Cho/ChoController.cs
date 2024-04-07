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
	private readonly BanchoSession _session;
	private readonly GeolocService _geoloc;
	private readonly OsuVersionService _version;
	private readonly Messages _messages;

	public ChoController(BanchoHandler bancho, GeolocService geoloc, OsuVersionService version, IOptions<Messages> messages)
	{
		_bancho = bancho;
		_session = BanchoSession.Instance;
		_geoloc = geoloc;
		_version = version;
		_messages = messages.Value;
	}
	
	[HttpPost("/")]
	public async Task<IActionResult> BanchoHandler()
	{
		var osuToken = Request.Headers["osu-token"];
		if (string.IsNullOrEmpty(osuToken))
			return await Login();

		var player = _session.GetPlayer(token: new Guid(osuToken!));
		if (player == null)
		{
			using var restartData = new ServerPackets();
			restartData.Notification("Server has restarted.");
			restartData.RestartServer(0);
			return restartData.GetContentResult();
		}
		
		await _bancho.ReadPackets(Request.Body, player);

		return Responses.BytesContentResult(player.Dequeue());
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