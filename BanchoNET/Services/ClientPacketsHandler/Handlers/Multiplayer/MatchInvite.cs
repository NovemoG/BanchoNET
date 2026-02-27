using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchInvite(User player, BinaryReader br)
	{
		var playerId = br.ReadInt32();
		
		var match = player.Match;
		if (match == null) return Task.CompletedTask;
		
		var target = playerService.GetPlayer(playerId);
		
		multiplayerCoordinator.InviteToLobby(player, target);
		
		player.LastActivityTime = DateTime.UtcNow;
		return Task.CompletedTask;
	}
}