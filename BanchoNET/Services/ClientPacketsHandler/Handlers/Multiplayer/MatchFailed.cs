using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchFailed(User player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return Task.CompletedTask;

		var slotId = match.GetPlayerSlotId(player);
		if (slotId == -1) throw new Exception("Player was not found in expected lobby");
		
		multiplayerCoordinator.EnqueueTo(match,
			new ServerPackets().MatchPlayerFailed(slotId).FinalizeAndGetContent(),
			toLobby: false
		);
		
		return Task.CompletedTask;
	}
}