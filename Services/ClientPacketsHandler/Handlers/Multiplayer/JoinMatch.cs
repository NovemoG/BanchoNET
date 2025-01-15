using BanchoNET.Models.Mongo;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using Action = BanchoNET.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task JoinMatch(Player player, BinaryReader br)
	{
		var lobbyId = br.ReadInt32();
		var password = br.ReadOsuString();
		
		if (lobbyId is < 0 or > ushort.MaxValue)
			return;
		
		if (player.Restricted)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while restricted.");
			player.Enqueue(joinFailPacket.GetContent());
			
			return;
		}

		if (player.Silenced)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while silenced.");
			player.Enqueue(joinFailPacket.GetContent());
			
			return;
		}
		
		var lobby = session.GetLobby((ushort)lobbyId);
		if (lobby == null)
		{
			Console.WriteLine($"[JoinMatch] {player.Username} tried to join a non-existent lobby ({lobbyId})");
			
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			player.Enqueue(joinFailPacket.GetContent());
			return;
		}

		if (lobby.BannedPlayers.Contains(player.Id))
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("You are banned from joining this lobby.");
			player.Enqueue(joinFailPacket.GetContent());
			return;
		}
		
		player.LastActivityTime = DateTime.Now;
		if (player.JoinMatch(lobby, password))
		{
			await histories.AddMatchAction(
				lobby.LobbyId,
				new ActionEntry
				{
					Action = Action.Joined,
					PlayerId = player.Id,
					Date = DateTime.Now
				});
		}

		player.SendBotMessage($"Match created by {player.Username} {lobby.MPLinkEmbed()}", "#multiplayer");
	}
}