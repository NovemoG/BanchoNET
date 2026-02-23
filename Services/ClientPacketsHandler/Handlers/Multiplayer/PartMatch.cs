using BanchoNET.Models.Mongo;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using Action = BanchoNET.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task PartMatch(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;

		if (player.LeaveMatch())
		{
			await histories.AddMatchAction(
				lobby!.LobbyId,
				new ActionEntry
				{
					Action = Action.Left,
					PlayerId = player.Id,
					Date = DateTime.Now
				});

			if (lobby.IsEmpty())
			{
				await histories.AddMatchAction(
					lobby.LobbyId,
					new ActionEntry
					{
						Action = Action.MatchDisbanded,
						PlayerId = player.Id,
						Date = DateTime.Now
					});
			}
		}
		player.LastActivityTime = DateTime.Now;
	}
}