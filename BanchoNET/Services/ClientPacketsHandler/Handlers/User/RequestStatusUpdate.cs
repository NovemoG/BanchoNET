using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task RequestStatusUpdate(User player, BinaryReader br)
	{
		playerService.EnqueueToPlayers(new ServerPackets()
			.UserStats(player)
			.FinalizeAndGetContent());
		
		player.LastActivityTime = DateTime.UtcNow;
		return Task.CompletedTask;
	}
}