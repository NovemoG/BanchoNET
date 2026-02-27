using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchChangeTeam(User player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null || match.Locked) return Task.CompletedTask;

		var slot = match.GetPlayerSlot(player)!;
		slot.Team = slot.Team == LobbyTeams.Blue ? LobbyTeams.Red : LobbyTeams.Blue;
		
		multiplayerCoordinator.EnqueueStateTo(match, false);
		return Task.CompletedTask;
	}
}