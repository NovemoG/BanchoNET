using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
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