using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchChangeMods(Player player, BinaryReader br)
	{
		var mods = (StableMods)br.ReadInt32();
		var match = player.Match;

		if (match == null) return Task.CompletedTask;
		if (match.Freemods)
		{
			if (player.Id == match.HostId)
				match.Mods = mods & StableMods.SpeedChangingMods;

			match.GetPlayerSlot(player)!.Mods = mods & ~StableMods.SpeedChangingMods;
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