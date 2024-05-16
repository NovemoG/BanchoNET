using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task JoinLobby(Player player, BinaryReader br)
	{
		player.JoinLobby();

		return Task.CompletedTask;
	}
}