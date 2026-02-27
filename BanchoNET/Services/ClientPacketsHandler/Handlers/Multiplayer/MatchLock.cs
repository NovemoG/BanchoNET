using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchLock(User player, BinaryReader br)
	{
		var slotId = br.ReadInt32();
		var match = player.Match;
		
		if (match == null || player.Id != match.HostId || slotId is < 0 or > 15) return Task.CompletedTask;

		var slot = match.Slots[slotId];

		if (slot.Status == SlotStatus.Locked)
			slot.Status = SlotStatus.Open;
		else
		{
			if (slot.Player?.Id == match.HostId)
				return Task.CompletedTask;
			
			//TODO check if osu handles this properly...

			slot.Status = SlotStatus.Locked;
		}

		multiplayerCoordinator.EnqueueStateTo(match);
		return Task.CompletedTask;
	}
}