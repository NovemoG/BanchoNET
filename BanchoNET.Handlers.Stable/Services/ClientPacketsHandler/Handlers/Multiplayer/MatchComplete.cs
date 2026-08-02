using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Stable.Multiplayer;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Handlers.Stable.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchComplete(Player player, BinaryReader br)
	{
		var match = player.Match;
		if (match == null) return;

		// A referee may have aborted the map out from under this client
		if (!match.InProgress) return;

		var slot = match.GetPlayerSlot(player)!;
		slot.Status = SlotStatus.Complete;

		if (match.Slots.Any(s => s.Status == SlotStatus.Playing))
		{
			// The first client to finish starts the clock on the rest of them, so a client that
			// hangs without ever reporting in cannot hold the lobby forever.
			if (!match.CompleteTimeout.IsArmed)
			{
				match.CompleteTimeout.Arm(AppSettings.MultiplayerMapCompleteTimeout, async () =>
				{
					logger.LogWarning($"Match {match.LobbyId} timed out waiting for clients to finish.");
					await multiplayerCoordinator.CompleteGame(match, forced: true);
				});
			}

			return;
		}

		await multiplayerCoordinator.CompleteGame(match);
	}
}
