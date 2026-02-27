using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Cho;

[ApiController]
[SubdomainAuthorize("c", "c4", "cho")]
public partial class ChoController(
	IPlayerService playerService,
	IPlayerCoordinator playerCoordinator,
	IChannelService channels,
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

		var player = playerService.GetPlayer(new Guid(osuToken));
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