using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task JoinLobby(Player player, BinaryReader br)
	{
		player.JoinLobby();

		return Task.CompletedTask;
	}
}