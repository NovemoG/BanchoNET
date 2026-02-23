using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task RequestStatusUpdate(Player player, BinaryReader br)
	{
		session.EnqueueToPlayers(new ServerPackets()
			.UserStats(player)
			.FinalizeAndGetContent());
		
		player.LastActivityTime = DateTime.Now;
		return Task.CompletedTask;
	}
}