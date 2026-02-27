using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task PartLobby(User player, BinaryReader br)
	{
		playerService.LeaveLobby(player);
		return Task.CompletedTask;
	}
}