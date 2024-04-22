using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchLock(Player player, BinaryReader br)
	{
		var slotId = br.ReadInt32();
		var lobby = player.Lobby;
		
		if (lobby == null) return Task.CompletedTask;
		if (player.Id != lobby.HostId) return Task.CompletedTask;
		if (slotId is < 0 or > 15) return Task.CompletedTask;

		var slot = lobby.Slots[slotId];

		if (slot.Status == SlotStatus.Locked)
			slot.Status = SlotStatus.Open;
		else
		{
			if (slot.Player?.Id == lobby.HostId)
				return Task.CompletedTask;
			
			//TODO check if osu handles this properly...

			slot.Status = SlotStatus.Locked;
		}

		lobby.EnqueueState();
		return Task.CompletedTask;
	}
}