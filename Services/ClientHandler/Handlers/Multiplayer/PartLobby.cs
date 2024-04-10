using BanchoNET.Objects.Players;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private Task PartLobby(Player player, BinaryReader br)
	{
		player.InLobby = false;
		return Task.CompletedTask;
	}
}