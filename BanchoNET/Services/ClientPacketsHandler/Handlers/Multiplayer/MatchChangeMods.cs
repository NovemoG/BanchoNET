using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchChangeMods(Player player, BinaryReader br)
	{
		var mods = (LegacyMods)br.ReadInt32();
		var match = player.Match;

		if (match == null) return Task.CompletedTask;
		if (match.Freemods)
		{
			if (player.Id == match.HostId)
				match.Mods = mods & LegacyMods.SpeedChangingMods;

			match.GetPlayerSlot(player)!.Mods = mods & ~LegacyMods.SpeedChangingMods;
		}
		else
		{
			if (player.Id != match.HostId)
				return Task.CompletedTask;

			match.Mods = mods;
		}

		multiplayerCoordinator.EnqueueStateTo(match);
		return Task.CompletedTask;
	}
}