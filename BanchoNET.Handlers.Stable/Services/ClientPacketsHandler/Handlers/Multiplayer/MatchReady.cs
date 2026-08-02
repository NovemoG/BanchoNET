using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Stable.Multiplayer;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Handlers.Stable.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchReady(Player player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return Task.CompletedTask;

		var slot = match.GetPlayerSlot(player)!;
		slot.Status = SlotStatus.Ready;
		
		multiplayerCoordinator.EnqueueStateTo(match, false);

		return Task.CompletedTask;
	}
}