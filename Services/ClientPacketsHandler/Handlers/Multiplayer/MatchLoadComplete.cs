using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchLoadComplete(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player)!;
		slot.Loaded = true;

		if (!lobby.Slots.Any(s => s is { Status: SlotStatus.Playing, Loaded: false }))
		{
			lobby.Enqueue(new ServerPackets().MatchAllPlayersLoaded().FinalizeAndGetContent(),
				toLobby: false);
		}

		return Task.CompletedTask;
	}
}