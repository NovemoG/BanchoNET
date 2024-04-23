using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchChangeSlot(Player player, BinaryReader br)
	{
		var slotId = br.ReadInt32();
		var lobby = player.Lobby;
		
		if (lobby == null) return Task.CompletedTask;
		if (lobby.Locked) return Task.CompletedTask;
		if (slotId is < 0 or > 15) return Task.CompletedTask;

		var targetSlot = lobby.Slots[slotId];
		if (targetSlot.Status != SlotStatus.Open) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player)!;

		targetSlot.CopyStatusFrom(slot);
		slot.Reset();
		
		lobby.EnqueueState();
		return Task.CompletedTask;
	}
}