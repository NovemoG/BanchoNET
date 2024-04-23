using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchChangeTeam(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		if (lobby.Locked) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player)!;
		slot.Team = slot.Team == LobbyTeams.Blue ? LobbyTeams.Red : LobbyTeams.Blue;
		
		lobby.EnqueueState(false);
		return Task.CompletedTask;
	}
}