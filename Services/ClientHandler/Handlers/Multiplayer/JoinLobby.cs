using BanchoNET.Objects.Players;
using BanchoNET.Packets;

namespace BanchoNET.Services;

public partial class PacketsHandler
{
	private Task JoinLobby(Player player, BinaryReader br)
	{
		player.InLobby = true;

		foreach (var lobby in _session.Lobbies)
		{
			using var newMatchPacket = new ServerPackets();
			newMatchPacket.NewMatch(lobby);
			player.Enqueue(newMatchPacket.GetContent());
		}

		return Task.CompletedTask;
	}
}