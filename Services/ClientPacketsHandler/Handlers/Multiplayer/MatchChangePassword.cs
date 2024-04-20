using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchChangePassword(Player player, BinaryReader br)
	{
		var matchData = br.ReadOsuMatch();

		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		if (player.Id != lobby.HostId) return Task.CompletedTask;

		lobby.Password = matchData.Password;
		lobby.EnqueueState();
		
		return Task.CompletedTask;
	}
}