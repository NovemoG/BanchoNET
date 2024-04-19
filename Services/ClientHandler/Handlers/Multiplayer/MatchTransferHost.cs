using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class PacketsHandler
{
	private Task MatchTransferHost(Player player, BinaryReader br)
	{
		var slotId = br.ReadInt32();

		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		if (player.Id != lobby.HostId) return Task.CompletedTask;
		if (slotId is < 0 or > 15) return Task.CompletedTask;

		var target = lobby.Slots[slotId].Player;
		if (target == null) return Task.CompletedTask;

		lobby.HostId = target.Id;

		using var hostTransferPacket = new ServerPackets();
		hostTransferPacket.MatchTransferHost();
		target.Enqueue(hostTransferPacket.GetContent());
		lobby.EnqueueState();
		
		return Task.CompletedTask;
	}
}