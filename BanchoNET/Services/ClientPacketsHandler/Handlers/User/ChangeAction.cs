using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ChangeAction(Player player, BinaryReader br)
	{
		var status = player.Status;

		status.Activity = (Activity)br.ReadByte();
		status.ActivityDescription = br.ReadOsuString();
		status.BeatmapMD5 = br.ReadOsuString();
		
		var mods = (StableMods)br.ReadInt32();
		var mode = (GameMode)br.ReadByte();

		if (mods.HasMod(StableMods.Relax))
		{
			if (mode == GameMode.VanillaMania)
				mods &= ~StableMods.Relax;
			else mode += 4;
		}
		else if (mods.HasMod(StableMods.Autopilot))
		{
			if (mode is GameMode.VanillaTaiko or GameMode.VanillaCatch or GameMode.VanillaMania)
				mods &= ~StableMods.Autopilot;
			else mode += 8;
		}

		status.CurrentMods = mods;
		status.Mode = mode;
		var beatmapId = status.BeatmapId = br.ReadInt32();

		if (beatmapId > 0)
			player.LastValidBeatmapId = beatmapId;
		
		playerService.EnqueueToPlayers(new ServerPackets()
			.UserStats(player)
			.FinalizeAndGetContent());

		player.LastActivityTime = DateTime.UtcNow;
		return Task.CompletedTask;
	}
}