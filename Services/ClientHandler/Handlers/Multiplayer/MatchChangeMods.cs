using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class PacketsHandler
{
	private Task MatchChangeMods(Player player, BinaryReader br)
	{
		var mods = (Mods)br.ReadInt32();
		var lobby = player.Lobby;

		if (lobby == null) return Task.CompletedTask;
		if (lobby.Freemods)
		{
			if (player.Id == lobby.HostId)
				lobby.Mods = mods & Mods.SpeedChangingMods;

			lobby.GetPlayerSlot(player).Mods = mods & ~Mods.SpeedChangingMods;
		}
		else
		{
			if (player.Id != lobby.HostId)
				return Task.CompletedTask;

			lobby.Mods = mods;
		}

		lobby.EnqueueState();
		return Task.CompletedTask;
	}
}