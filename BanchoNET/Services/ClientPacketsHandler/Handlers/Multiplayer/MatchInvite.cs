using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchInvite(Player player, BinaryReader br)
	{
		var playerId = br.ReadInt32();
		
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		
		var target = session.GetPlayerById(playerId);
		
		MultiplayerExtensions.InviteToLobby(player, target);
		
		player.LastActivityTime = DateTime.Now;
		return Task.CompletedTask;
	}
}