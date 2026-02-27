using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;
using Action = BanchoNET.Core.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task PartMatch(User player, BinaryReader br)
	{
		var match = player.Match;
		
		if (multiplayerCoordinator.LeavePlayer(player))
		{
			await histories.AddMatchAction(
				match!.LobbyId,
				new ActionEntry
				{
					Action = Action.Left,
					PlayerId = player.Id,
					Date = DateTime.UtcNow
				});

			if (match.IsEmpty())
			{
				await histories.AddMatchAction(
					match.LobbyId,
					new ActionEntry
					{
						Action = Action.MatchDisbanded,
						PlayerId = player.Id,
						Date = DateTime.UtcNow
					});
			}
		}
		player.LastActivityTime = DateTime.UtcNow;
	}
}