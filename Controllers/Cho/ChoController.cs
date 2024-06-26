﻿using BanchoNET.Attributes;
using BanchoNET.Packets;
using BanchoNET.Services;
using BanchoNET.Services.ClientPacketsHandler;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Cho;

[ApiController]
[SubdomainAuthorize("c", "c4", "cho")]
public partial class ChoController(
	GeolocService geoloc,
	OsuVersionService version,
	PlayersRepository players,
	ClientRepository client,
	ClientPacketsHandler clientPackets,
	MessagesRepository messages)
	: ControllerBase
{
	private readonly BanchoSession _session = BanchoSession.Instance;
	
	
	[HttpPost("/")]
	public async Task<IActionResult> BanchoHandler()
	{
		var osuToken = Request.Headers["osu-token"].ToString();
		if (string.IsNullOrEmpty(osuToken))
			return await Login();

		var player = _session.GetPlayerByToken(new Guid(osuToken));
		if (player == null)
		{
			using var restartData = new ServerPackets();
			restartData.Notification("Server has restarted.");
			restartData.RestartServer(0);
			return restartData.GetContentResult();
		}
		
		await clientPackets.ReadPackets(Request.Body, player);

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