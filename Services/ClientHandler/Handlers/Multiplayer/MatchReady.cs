using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private Task MatchReady(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player);
		slot.Status = SlotStatus.Ready;
		lobby.EnqueueState(false);

		return Task.CompletedTask;
	}
}