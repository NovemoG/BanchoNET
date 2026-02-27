using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchComplete(User player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return;

		var slots = match.Slots;
		
		var slot = match.GetPlayerSlot(player)!;
		slot.Status = SlotStatus.Complete;
		
		// assigning UtcNow instead of Now because the ClientTime date of score is in UTC
		if (match.MapFinishDate == DateTime.MinValue)
			match.MapFinishDate = DateTime.UtcNow;
        
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
			MapFinishDate = match.MapFinishDate,
			Match = match
		});
		
		match.UnreadyPlayers(SlotStatus.Complete);
		match.ResetPlayersLoadedStatuses();
		match.InProgress = false;
		
		multiplayerCoordinator.EnqueueTo(match,
			new ServerPackets().MatchComplete().FinalizeAndGetContent(),
			notPlayingIds,
			false
		);
		multiplayerCoordinator.EnqueueStateTo(match);
		
		// reset map finish date
		match.MapFinishDate = DateTime.MinValue;
	}
}