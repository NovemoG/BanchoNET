using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchLoadComplete(User player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return Task.CompletedTask;

		var slot = match.GetPlayerSlot(player)!;
		slot.Loaded = true;

		if (!match.Slots.Any(s => s is { Status: SlotStatus.Playing, Loaded: false }))
		{
			multiplayerCoordinator.EnqueueTo(match,
				new ServerPackets().MatchAllPlayersLoaded().FinalizeAndGetContent(),
				toLobby: false
			);
		}

		return Task.CompletedTask;
	}
}