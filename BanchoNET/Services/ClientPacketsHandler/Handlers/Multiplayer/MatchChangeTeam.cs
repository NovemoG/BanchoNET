using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

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