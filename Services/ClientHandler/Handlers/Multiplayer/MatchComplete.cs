using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class PacketsHandler
{
	private Task MatchComplete(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slots = lobby.Slots;
		
		var slot = lobby.GetPlayerSlot(player);
		slot.Status = SlotStatus.Complete;
        
		if (slots.Any(s => s.Status == SlotStatus.Playing))
			return Task.CompletedTask;

		var notPlayingIds = new List<int>();

		foreach (var s in slots)
		{
			if (s.Player == null) continue;
			
			if (s.Status != SlotStatus.Complete)
				notPlayingIds.Add(s.Player.Id);
		}
		
		lobby.UnreadyPlayers(SlotStatus.Complete);
		lobby.ResetPlayersLoadedStatuses();
		lobby.InProgress = false;

		using var matchCompletedPacket = new ServerPackets();
		matchCompletedPacket.MatchComplete();
		lobby.Enqueue(matchCompletedPacket.GetContent(), notPlayingIds, false);
		lobby.EnqueueState();
		
		return Task.CompletedTask;
	}
}