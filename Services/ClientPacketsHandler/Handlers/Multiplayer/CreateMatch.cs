using BanchoNET.Models.Mongo;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils.Extensions;
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