using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchChangeSlot(User player, BinaryReader br)
	{
		var slotId = br.ReadInt32();
		var match = player.Match;
		
		if (match == null || match.Locked || slotId is < 0 or > 15) return Task.CompletedTask;

		var targetSlot = match.Slots[slotId];
		if (targetSlot.Status != SlotStatus.Open) return Task.CompletedTask;

		var slot = match.GetPlayerSlot(player)!;

		targetSlot.CopyStatusFrom(slot);
		slot.Reset();
		
		multiplayerCoordinator.EnqueueStateTo(match);
		return Task.CompletedTask;
	}
}