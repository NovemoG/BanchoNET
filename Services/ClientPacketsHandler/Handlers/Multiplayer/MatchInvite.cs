using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchInvite(Player player, BinaryReader br)
	{
		var playerId = br.ReadInt32();
		
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		
		var target = _session.GetPlayerById(playerId);
		
		MultiplayerExtensions.InviteToLobby(player, target);
		
		player.LastActivityTime = DateTime.Now;
		return Task.CompletedTask;
	}
}