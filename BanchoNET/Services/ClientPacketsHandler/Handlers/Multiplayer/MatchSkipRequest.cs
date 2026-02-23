using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchSkipRequest(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player)!;
		slot.Skipped = true;
		
		lobby.Enqueue(new ServerPackets()
			.MatchPlayerSkipped(player.Id)
			.FinalizeAndGetContent());

		foreach (var s in lobby.Slots)
			if (s is { Status: SlotStatus.Playing, Skipped: false })
				return Task.CompletedTask;
		
		lobby.Enqueue(new ServerPackets().MatchSkip().FinalizeAndGetContent(),
			toLobby: false);
		
		return Task.CompletedTask;
	}
}