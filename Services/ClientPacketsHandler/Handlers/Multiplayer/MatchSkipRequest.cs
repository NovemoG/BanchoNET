using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchSkipRequest(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player);
		slot.Skipped = true;
		
		using var playerSkippedPacket = new ServerPackets();
		playerSkippedPacket.MatchPlayerSkipped(player.Id);
		lobby.Enqueue(playerSkippedPacket.GetContent());

		foreach (var s in lobby.Slots)
			if (s is { Status: SlotStatus.Playing, Skipped: false })
				return Task.CompletedTask;

		using var matchSkipPacket = new ServerPackets();
		matchSkipPacket.MatchSkip();
		lobby.Enqueue(matchSkipPacket.GetContent(), toLobby: false);
		
		return Task.CompletedTask;
	}
}