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
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.Notification("Multiplayer is not available while restricted.")
				.FinalizeAndGetContent());
			return;
		}

		if (player.Silenced)
		{
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.Notification("Multiplayer is not available while silenced.")
				.FinalizeAndGetContent());
			return;
		}
		
		var lobby = session.GetLobby((ushort)lobbyId);
		if (lobby == null)
		{
			Console.WriteLine($"[JoinMatch] {player.Username} tried to join a non-existent lobby ({lobbyId})");
			
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.FinalizeAndGetContent());
			return;
		}

		if (lobby.BannedPlayers.Contains(player.Id))
		{
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.Notification("You are banned from joining this lobby.")
				.FinalizeAndGetContent());
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