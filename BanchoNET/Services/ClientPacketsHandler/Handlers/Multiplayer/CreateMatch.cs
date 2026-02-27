using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;
using Action = BanchoNET.Core.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task CreateMatch(User player, BinaryReader br)
	{
		var matchData = br.ReadOsuMatch();
		matchData.CreatorId = matchData.HostId;
		
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

		await multiplayerCoordinator.CreateMatchAsync(matchData, player);
		player.LastActivityTime = DateTime.UtcNow;

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
				Date = DateTime.UtcNow
			});
	}
}