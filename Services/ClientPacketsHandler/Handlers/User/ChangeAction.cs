using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ChangeAction(Player player, BinaryReader br)
	{
		var status = player.Status;

		status.Activity = (Activity)br.ReadByte();
		status.ActivityDescription = br.ReadOsuString();
		status.BeatmapMD5 = br.ReadOsuString();
		
		var mods = (Mods)br.ReadInt32();
		var mode = (GameMode)br.ReadByte();

		if (mods.HasMod(Mods.Relax))
		{
			if (mode == GameMode.VanillaMania)
				mods &= ~Mods.Relax;
			else mode += 4;
		}
		else if (mods.HasMod(Mods.Autopilot))
		{
			if (mode is GameMode.VanillaTaiko or GameMode.VanillaCatch or GameMode.VanillaMania)
				mods &= ~Mods.Autopilot;
			else mode += 8;
		}

		status.CurrentMods = mods;
		status.Mode = mode;
		var beatmapId = status.BeatmapId = br.ReadInt32();

		if (beatmapId > 0)
			player.LastValidBeatmapId = beatmapId;
		
		using var packet = new ServerPackets();
		packet.UserStats(player);
		_session.EnqueueToPlayers(packet.GetContent());

		player.LastActivityTime = DateTime.Now;
		return Task.CompletedTask;
	}
}