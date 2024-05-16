using BanchoNET.Models.Dtos;
using BanchoNET.Models.Mongo;
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
		
		// assigning UtcNow instead of Now because the ClientTime date of score is in UTC
		if (lobby.MapFinishDate == DateTime.MinValue)
			lobby.MapFinishDate = DateTime.UtcNow;
        
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
					.Select(s => s.Player!.Id),
				lobby.MapFinishDate));
		
		lobby.UnreadyPlayers(SlotStatus.Complete);
		lobby.ResetPlayersLoadedStatuses();
		lobby.InProgress = false;

		using var matchCompletedPacket = new ServerPackets();
		matchCompletedPacket.MatchComplete();
		lobby.Enqueue(matchCompletedPacket.GetContent(), notPlayingIds, false);
		lobby.EnqueueState();
		
		var scoreEntries = submittedScores.Select(score => new ScoreEntry
			{
				Accuracy = score.Acc,
				Grade = score.Grade,
				Gekis = score.Gekis,
				Count300 = score.Count300,
				Katus = score.Katus,
				Count100 = score.Count100,
				Count50 = score.Count50,
				Misses = score.Misses,
				MaxCombo = score.MaxCombo,
				Mods = score.Mods,
				PlayerId = score.PlayerId,
				TotalScore = score.Score,
				Failed = score.Status == 0,
				Team = (byte)lobby.GetPlayerSlot(score.PlayerId)!.Team
			})
			.ToList();
		
		await histories.MapCompleted(lobby.LobbyId, scoreEntries);

		lobby.MapFinishDate = DateTime.MinValue;
	}
}