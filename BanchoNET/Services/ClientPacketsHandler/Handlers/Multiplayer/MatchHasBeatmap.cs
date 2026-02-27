using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchHasBeatmap(User player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return Task.CompletedTask;

		var slot = match.GetPlayerSlot(player)!;
		slot.Status = SlotStatus.NotReady;
		
		multiplayerCoordinator.EnqueueStateTo(match, false);
		return Task.CompletedTask;
	}
}