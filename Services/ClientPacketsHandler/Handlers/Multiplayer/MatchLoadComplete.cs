using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchLoadComplete(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player);
		slot.Loaded = true;

		if (!lobby.Slots.Any(s => s is { Status: SlotStatus.Playing, Loaded: false }))
		{
			using var playersLoadedPacket = new ServerPackets();
			playersLoadedPacket.MatchAllPlayersLoaded();
			lobby.Enqueue(playersLoadedPacket.GetContent(), toLobby: false);
		}

		return Task.CompletedTask;
	}
}