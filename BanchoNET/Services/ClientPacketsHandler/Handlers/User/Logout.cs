using BanchoNET.Core.Models.Users;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task Logout(User player, BinaryReader br)
	{
		br.ReadInt32();
		
		if (playerCoordinator.LogoutPlayer(player))
			await players.UpdateLatestActivity(player);
	}
}