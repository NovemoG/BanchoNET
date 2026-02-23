using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;
using Action = BanchoNET.Core.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchTransferHost(Player player, BinaryReader br)
	{
		var slotId = br.ReadInt32();

		var lobby = player.Lobby;
		if (lobby == null) return;
		if (player.Id != lobby.HostId) return;
		if (slotId is < 0 or > 15) return;

		var target = lobby.Slots[slotId].Player;
		if (target == null) return;

		lobby.HostId = target.Id;
		
		lobby.Enqueue(new ServerPackets()
			.MatchTransferHost()
			.FinalizeAndGetContent());
		lobby.EnqueueState();

		await histories.AddMatchAction(
			lobby.LobbyId,
			new ActionEntry
			{
				Action = Action.HostChanged,
				PlayerId = target.Id,
				Date = DateTime.Now
			});
	}
}