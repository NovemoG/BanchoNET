using BanchoNET.Objects.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task PartLobby(Player player, BinaryReader br)
	{
		session.LeaveLobby(player);
		return Task.CompletedTask;
	}
}