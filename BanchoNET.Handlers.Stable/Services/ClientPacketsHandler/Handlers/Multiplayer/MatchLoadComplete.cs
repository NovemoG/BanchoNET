using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Stable.Multiplayer;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Handlers.Stable.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchLoadComplete(Player player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return Task.CompletedTask;

		var slot = match.GetPlayerSlot(player)!;
		slot.Loaded = true;

		if (!match.Slots.Any(s => s is { Status: SlotStatus.Playing, Loaded: false }))
			multiplayerCoordinator.AllPlayersLoaded(match);

		return Task.CompletedTask;
	}
}
