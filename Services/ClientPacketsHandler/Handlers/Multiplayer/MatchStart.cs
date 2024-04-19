using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchStart(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		if (player.Id != lobby.HostId) return Task.CompletedTask;

		lobby.Start();
		return Task.CompletedTask;
	}
}