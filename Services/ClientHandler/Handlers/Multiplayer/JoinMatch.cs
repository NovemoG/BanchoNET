using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class PacketsHandler
{
	private Task JoinMatch(Player player, BinaryReader br)
	{
		var lobbyId = br.ReadInt32();
		var password = br.ReadOsuString();
		
		if (player.Restricted)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while restricted.");
			player.Enqueue(joinFailPacket.GetContent());
			
			return Task.CompletedTask;
		}

		if (player.Silenced)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while silenced.");
			player.Enqueue(joinFailPacket.GetContent());
			
			return Task.CompletedTask;
		}
		
		var lobby = _session.GetLobby((ushort)lobbyId);
		if (lobby == null)
		{
			Console.WriteLine($"[JoinMatch] {player.Username} tried to join a non-existent lobby ({lobbyId})");
			
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			player.Enqueue(joinFailPacket.GetContent());
			return Task.CompletedTask;
		}
		
		player.LastActivityTime = DateTime.Now;
		player.JoinMatch(lobby, password);

		return Task.CompletedTask;
	}
}