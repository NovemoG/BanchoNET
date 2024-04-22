using BanchoNET.Objects.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task PartLobby(Player player, BinaryReader br)
	{
		player.InLobby = false;
		return Task.CompletedTask;
	}
}