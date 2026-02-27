using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchSkipRequest(User player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return Task.CompletedTask;

		var slot = match.GetPlayerSlot(player)!;
		slot.Skipped = true;

		multiplayerCoordinator.EnqueueTo(match,
			new ServerPackets()
				.MatchPlayerSkipped(player.Id)
				.FinalizeAndGetContent()
		);

		foreach (var s in match.Slots)
			if (s is { Status: SlotStatus.Playing, Skipped: false })
				return Task.CompletedTask;
		
		multiplayerCoordinator.EnqueueTo(match,
			new ServerPackets().MatchSkip().FinalizeAndGetContent(),
			toLobby: false
		);
		
		return Task.CompletedTask;
	}
}