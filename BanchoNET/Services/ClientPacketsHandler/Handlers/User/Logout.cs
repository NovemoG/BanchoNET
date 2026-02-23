using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task Logout(Player player, BinaryReader br)
	{
		br.ReadInt32();
		
		if (session.LogoutPlayer(player))
			await players.UpdateLatestActivity(player);
	}
}