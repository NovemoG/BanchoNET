using BanchoNET.Models.Mongo;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchStart(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return;
		if (player.Id != lobby.HostId) return;

		lobby.Start();

		await histories.MapStarted(
			lobby.LobbyId,
			new ScoresEntry
			{
				StartDate = DateTime.Now,
				GameMode = (byte)lobby.Mode,
				WinCondition = (byte)lobby.WinCondition,
				LobbyType = (byte)lobby.Type,
				LobbyMods = lobby.Freemods ? 0 : (int)lobby.Mods,
				BeatmapId = lobby.BeatmapId,
				BeatmapName = lobby.BeatmapName,
				Values = []
			});
	}
}