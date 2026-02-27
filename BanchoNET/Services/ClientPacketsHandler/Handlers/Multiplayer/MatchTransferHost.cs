using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using Action = BanchoNET.Core.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchTransferHost(User player, BinaryReader br)
	{
		var slotId = br.ReadInt32();

		var match = player.Match;
		if (match == null) return;
		if (player.Id != match.HostId) return;
		if (slotId is < 0 or > 15) return;

		var target = match.Slots[slotId].Player;
		if (target == null) return;

		match.HostId = target.Id;
		
		multiplayerCoordinator.EnqueueTo(match,
			new ServerPackets()
				.MatchTransferHost()
				.FinalizeAndGetContent()
		);
		multiplayerCoordinator.EnqueueStateTo(match);

		await histories.AddMatchAction(
			match.LobbyId,
			new ActionEntry
			{
				Action = Action.HostChanged,
				PlayerId = target.Id,
				Date = DateTime.UtcNow
			});
	}
}