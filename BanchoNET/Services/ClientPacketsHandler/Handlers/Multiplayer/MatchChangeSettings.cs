using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchChangeSettings(User player, BinaryReader br)
	{
		var matchData = br.ReadOsuMatch();
		
		if (!player.InMatch) return;
		
		var match = player.Match!;
		if (match.HostId != player.Id) return;

		var host = match.GetHostSlot();
		var slots = match.Slots;
		
		if (matchData.Freemods != match.Freemods)
		{
			match.Freemods = matchData.Freemods;

			if (matchData.Freemods)
			{
				foreach (var slot in slots)
                {
                	if (slot.Player == null) continue;
                	slot.Mods = match.Mods & ~Mods.SpeedChangingMods;
                }
                
                match.Mods &= Mods.SpeedChangingMods;
			}
			else
			{
				match.Mods &= Mods.SpeedChangingMods;
				match.Mods |= host.Mods;
				
				foreach (var slot in slots)
				{
					if (slot.Player == null) continue;
					slot.Mods = Mods.None;
				}
			}
		}

		if (matchData.BeatmapId == -1)
		{
			match.UnreadyPlayers();
			match.PreviousBeatmapId = matchData.LobbyId;

			match.BeatmapId = -1;
			match.BeatmapName = "";
			match.BeatmapMD5 = "";
		}
		else if (match.BeatmapId == -1)
		{
			if (match.PreviousBeatmapId != matchData.BeatmapId)
				match.Chat.SendBotMessage($"Selected: {matchData.MapEmbed()}", playerService.BanchoBot);

			var beatmap = await beatmaps.GetBeatmap(matchData.BeatmapMD5);

			if (beatmap != null)
			{
				match.BeatmapId = beatmap.Id;
				match.BeatmapName = beatmap.FullName();
				match.BeatmapMD5 = beatmap.MD5;
				match.Mode = host.Player!.Status.Mode.AsVanilla();
			}
			else
			{
				match.BeatmapId = matchData.BeatmapId;
				match.BeatmapName = matchData.BeatmapName;
				match.BeatmapMD5 = matchData.BeatmapMD5;
				match.Mode = matchData.Mode;
			}
		}

		if (match.Type != matchData.Type)
		{
			var newType = matchData.Type is LobbyType.HeadToHead or LobbyType.TagCoop
				? LobbyTeams.Neutral
				: LobbyTeams.Red;

			foreach (var slot in slots)
				if (slot.Player != null)
					slot.Team = newType;

			match.Type = matchData.Type;
		}

		if (match.WinCondition != matchData.WinCondition)
			match.WinCondition = matchData.WinCondition;
		
		match.Name = matchData.Name;
		
		multiplayerCoordinator.EnqueueStateTo(match);
	}
}