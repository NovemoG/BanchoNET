using BanchoNET.Core.Models.Players;

namespace BanchoNET.Handlers.Stable.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task Logout(Player player, BinaryReader br)
	{
		br.ReadInt32();
		
		if (await playerCoordinator.LogoutPlayer(player))
			await players.UpdateLatestActivity(player);
	}
}