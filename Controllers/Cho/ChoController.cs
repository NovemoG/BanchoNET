using BanchoNET.Abstractions.Services;
using BanchoNET.Attributes;
using BanchoNET.Packets;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Cho;

[ApiController]
[SubdomainAuthorize("c", "c4", "cho")]
public partial class ChoController(
	IBanchoSession session,
	IGeolocService geoloc,
	IOsuVersionService version,
	PlayersRepository players,
	ClientRepository client,
	IClientPacketsHandler clientPackets,
	MessagesRepository messages)
	: ControllerBase
{
	[HttpPost("/")]
	public async Task<IActionResult> BanchoHandler()
	{
		var osuToken = Request.Headers["osu-token"].ToString();
		if (string.IsNullOrEmpty(osuToken))
			return await Login();

		var player = session.GetPlayerByToken(new Guid(osuToken));
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
}