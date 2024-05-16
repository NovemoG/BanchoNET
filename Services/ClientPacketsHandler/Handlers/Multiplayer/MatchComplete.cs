using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchComplete(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return;

		var slots = lobby.Slots;
		
		var slot = lobby.GetPlayerSlot(player)!;
		slot.Status = SlotStatus.Complete;
        
		if (slots.Any(s => s.Status == SlotStatus.Playing))
			return;

		var notPlayingIds = new List<int>();

		foreach (var s in slots)
		{
			if (s.Player == null) continue;
			
			if (s.Status != SlotStatus.Complete)
				notPlayingIds.Add(s.Player.Id);
		}

		var submittedScores = new List<ScoreDto>(
			await scores.GetPlayersRecentScores(
				lobby.Slots
					.Where(s => s.Status == SlotStatus.Complete)
					.Select(s => s.Player!.Id)));
		
		lobby.UnreadyPlayers(SlotStatus.Complete);
		lobby.ResetPlayersLoadedStatuses();
		lobby.InProgress = false;

		using var matchCompletedPacket = new ServerPackets();
		matchCompletedPacket.MatchComplete();
		lobby.Enqueue(matchCompletedPacket.GetContent(), notPlayingIds, false);
		lobby.EnqueueState();
		
		if (submittedScores.Count > 0)
			await histories.MapCompleted(lobby.LobbyId, submittedScores);
	}
}