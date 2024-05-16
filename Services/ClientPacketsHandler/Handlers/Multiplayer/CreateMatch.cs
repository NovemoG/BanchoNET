using BanchoNET.Models.Mongo;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Action = BanchoNET.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task CreateMatch(Player player, BinaryReader br)
	{
		var matchData = br.ReadOsuMatch();
		matchData.CreatorId = matchData.HostId;
		
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
		
		MultiplayerExtensions.CreateLobby(matchData, player, await histories.GetMatchId());
		player.LastActivityTime = DateTime.Now;

		await histories.InsertMatchHistory(new MultiplayerMatch
		{
			MatchId = matchData.LobbyId,
			Name = matchData.Name,
			Actions = [],
			Scores = [],
		});

		await histories.AddMatchAction(
			matchData.LobbyId,
			new ActionEntry
			{
				Action = Action.MatchCreated,
				PlayerId = player.Id,
				Date = DateTime.Now
			});
	}
}