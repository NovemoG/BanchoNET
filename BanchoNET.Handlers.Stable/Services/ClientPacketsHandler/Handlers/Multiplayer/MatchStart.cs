using BanchoNET.Core.Models.Players;

namespace BanchoNET.Handlers.Stable.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchStart(Player player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return;
		if (player.Id != match.HostId) return;
		if (match.BeatmapId < 1 || string.IsNullOrEmpty(match.BeatmapMD5)) return;

		await multiplayerCoordinator.StartMatch(match);
	}
}
