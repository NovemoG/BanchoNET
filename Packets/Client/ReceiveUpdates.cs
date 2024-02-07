using BanchoNET.Objects.Players;

namespace BanchoNET.Packets;

public partial class ClientPackets
{
	private static void ReceiveUpdates(Player player, BinaryReader br)
	{
		var value = br.ReadInt32();
		
		player.PresenceFilter = (PresenceFilter)value;
	}
}