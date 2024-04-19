using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class PacketsHandler
{
	private Task MatchNoBeatmap(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player);
		slot.Status = SlotStatus.NoMap;
		
		lobby.EnqueueState();
		return Task.CompletedTask;
	}
}