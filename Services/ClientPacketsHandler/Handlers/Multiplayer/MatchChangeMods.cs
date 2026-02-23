using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
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

			lobby.GetPlayerSlot(player)!.Mods = mods & ~Mods.SpeedChangingMods;
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