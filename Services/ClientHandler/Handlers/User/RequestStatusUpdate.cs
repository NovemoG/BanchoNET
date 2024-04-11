using BanchoNET.Objects.Players;
using BanchoNET.Packets;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private Task RequestStatusUpdate(Player player, BinaryReader br)
	{
		using var packet = new ServerPackets();
		packet.UserStats(player);
		_session.EnqueueToPlayers(packet.GetContent());
		
		player.LastActivityTime = DateTime.Now;
		return Task.CompletedTask;
	}
}