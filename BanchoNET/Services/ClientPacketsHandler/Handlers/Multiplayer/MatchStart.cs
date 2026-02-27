using BanchoNET.Core.Models.Mongo;
using BanchoNET.Core.Models.Users;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchStart(User player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return;
		if (player.Id != match.HostId) return;

		multiplayerCoordinator.StartMatch(match);

		await histories.MapStarted(
			match.LobbyId,
			new ScoresEntry
			{
				StartDate = DateTime.UtcNow,
				GameMode = (byte)match.Mode,
				WinCondition = (byte)match.WinCondition,
				LobbyType = (byte)match.Type,
				LobbyMods = match.Freemods ? 0 : (int)match.Mods,
				BeatmapId = match.BeatmapId,
				BeatmapName = match.BeatmapName,
				Values = []
			});
	}
}