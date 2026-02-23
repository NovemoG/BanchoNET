using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchFailed(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slotId = lobby.GetPlayerSlotId(player);
		if (slotId == -1) throw new Exception("Player was not found in expected lobby");
		
		lobby.Enqueue(new ServerPackets().MatchPlayerFailed(slotId).FinalizeAndGetContent(),
			toLobby: false);
		
		return Task.CompletedTask;
	}
}