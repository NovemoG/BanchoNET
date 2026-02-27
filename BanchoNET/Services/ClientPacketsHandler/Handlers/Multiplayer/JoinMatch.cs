using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;
using Action = BanchoNET.Core.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task JoinMatch(User player, BinaryReader br)
	{
		var lobbyId = br.ReadInt32();
		var password = br.ReadOsuString();
		
		if (lobbyId is < 0 or > ushort.MaxValue)
			return;
		
		if (player.IsRestricted)
		{
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.Notification("Multiplayer is not available while restricted.")
				.FinalizeAndGetContent());
			return;
		}

		if (player.IsSilenced)
		{
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.Notification("Multiplayer is not available while silenced.")
				.FinalizeAndGetContent());
			return;
		}
		
		var match = multiplayer.GetMatch((ushort)lobbyId);
		if (match == null)
		{
			Console.WriteLine($"[JoinMatch] {player.Username} tried to join a non-existent lobby ({lobbyId})");
			
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.FinalizeAndGetContent());
			return;
		}

		if (match.BannedPlayers.Contains(player.Id))
		{
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.Notification("You are banned from joining this lobby.")
				.FinalizeAndGetContent());
			return;
		}
		
		player.LastActivityTime = DateTime.UtcNow;
		if (multiplayerCoordinator.JoinPlayer(match.Id, password, player))
		{
			await histories.AddMatchAction(
				match.LobbyId,
				new ActionEntry
				{
					Action = Action.Joined,
					PlayerId = player.Id,
					Date = DateTime.UtcNow
				});
		}

		playerService.SendBotMessageTo(player,
			$"Match created by {player.Username} {match.MPLinkEmbed()}", "#multiplayer"
		);
	}
}