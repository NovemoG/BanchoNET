using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchFailed(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slotId = lobby.GetPlayerSlotId(player);
		if (slotId == -1) throw new Exception("Player was not found in expected lobby");
		
		using var playerFailedPacket = new ServerPackets();
		playerFailedPacket.MatchPlayerFailed(slotId);
		lobby.Enqueue(playerFailedPacket.GetContent(), toLobby: false);
		
		return Task.CompletedTask;
	}
}