using BanchoNET.Abstractions.Repositories;
using BanchoNET.Abstractions.Services;
using BanchoNET.Attributes;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Cho;

[ApiController]
[SubdomainAuthorize("c", "c4", "cho")]
public partial class ChoController(
	IBanchoSession session,
	IGeolocService geoloc,
	IOsuVersionService version,
	IPlayersRepository players,
	IClientsRepository clients,
	IClientPacketsHandler clientPackets,
	IMessagesRepository messages,
	ILogger logger)
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
			return new ServerPackets()
				.Notification("Server has restarted.")
				.RestartServer(0)
				.FinalizeAndGetContentResult();
		}
		
		await clientPackets.ReadPackets(Request.Body, player);

		return Responses.BytesContentResult(player.Dequeue());
	}
}