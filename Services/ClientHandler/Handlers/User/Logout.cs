using BanchoNET.Objects.Players;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private async Task Logout(Player player, BinaryReader br)
	{
		br.ReadInt32();
		
		if (_session.LogoutPlayer(player))
			await UpdateLatestActivity(player);
	}
}