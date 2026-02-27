using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchNoBeatmap(User player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return Task.CompletedTask;

		var slot = match.GetPlayerSlot(player)!;
		slot.Status = SlotStatus.NoMap;
		
		multiplayerCoordinator.EnqueueStateTo(match);
		return Task.CompletedTask;
	}
}