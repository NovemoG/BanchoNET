using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task JoinLobby(User player, BinaryReader br)
	{
		multiplayerCoordinator.JoinLobby(player);

		return Task.CompletedTask;
	}
}