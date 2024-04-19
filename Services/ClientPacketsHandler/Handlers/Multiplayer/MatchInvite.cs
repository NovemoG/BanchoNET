using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchInvite(Player player, BinaryReader br)
	{
		var playerId = br.ReadInt32();
		
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;
		
		var target = _session.GetPlayer(playerId);
		
		if (target == null) return Task.CompletedTask;
		if (target.IsBot)
		{
			player.SendBotMessage("I'm too busy right now! Maybe later \ud83d\udc7c");
			return Task.CompletedTask;
		}

		using var invitePacket = new ServerPackets();
		invitePacket.MatchInvite(player, target.Username);
		target.Enqueue(invitePacket.GetContent());
		
		player.LastActivityTime = DateTime.Now;
		
		Console.WriteLine($"[MatchInvite] {player.Username} invited {target.Username} to their match.");
		return Task.CompletedTask;
	}
}