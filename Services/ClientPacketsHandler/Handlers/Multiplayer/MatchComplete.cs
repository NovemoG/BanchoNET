using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils.Extensions;

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
		
		await scoresQueue.EnqueueJobAsync(new ScoreRequestDto
		{
			Slots = slots
				.Where(s => s.Status == SlotStatus.Complete)
				.Select(s => s.Player!.Id)
				.ToList(),
			MapFinishDate = lobby.MapFinishDate,
			Lobby = lobby
		});
		
		lobby.UnreadyPlayers(SlotStatus.Complete);
		lobby.ResetPlayersLoadedStatuses();
		lobby.InProgress = false;
		
		lobby.Enqueue(new ServerPackets().MatchComplete().FinalizeAndGetContent(),
			notPlayingIds,
			false);
		lobby.EnqueueState();
		
		// reset map finish date
		lobby.MapFinishDate = DateTime.MinValue;
	}
}