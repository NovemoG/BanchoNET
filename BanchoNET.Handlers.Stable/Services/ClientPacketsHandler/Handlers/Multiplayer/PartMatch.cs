using BanchoNET.Core.Models.Players;

namespace BanchoNET.Handlers.Stable.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task PartMatch(Player player, BinaryReader br)
	{
		await multiplayerCoordinator.LeavePlayer(player);

		player.LastActivityTime = DateTime.UtcNow;
	}
}
