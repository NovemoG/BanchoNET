using BanchoNET.Objects.Players;

namespace BanchoNET.Packets;

public partial class ClientPackets
{
	private static void RequestStatusUpdate(Player player, BinaryReader br)
	{
		using var packet = new ServerPackets();
		packet.UserStats(player);
		Session.EnqueueToPlayers(packet.GetContent());
	}
}