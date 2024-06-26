﻿using BanchoNET.Objects;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchChangeSettings(Player player, BinaryReader br)
	{
		var matchData = br.ReadOsuMatch();
		
		if (!player.InMatch) return;
		
		var lobby = player.Lobby!;
		if (lobby.HostId != player.Id) return;

		var host = lobby.GetHostSlot();
		var slots = lobby.Slots;
		
		if (matchData.Freemods != lobby.Freemods)
		{
			lobby.Freemods = matchData.Freemods;

			if (matchData.Freemods)
			{
				foreach (var slot in slots)
                {
                	if (slot.Player == null) continue;
                	slot.Mods = lobby.Mods & ~Mods.SpeedChangingMods;
                }
                
                lobby.Mods &= Mods.SpeedChangingMods;
			}
			else
			{
				lobby.Mods &= Mods.SpeedChangingMods;
				lobby.Mods |= host.Mods;
				
				foreach (var slot in slots)
				{
					if (slot.Player == null) continue;
					slot.Mods = Mods.None;
				}
			}
		}

		if (matchData.BeatmapId == -1)
		{
			lobby.UnreadyPlayers();
			lobby.PreviousBeatmapId = matchData.LobbyId;

			lobby.BeatmapId = -1;
			lobby.BeatmapName = "";
			lobby.BeatmapMD5 = "";
		}
		else if (lobby.BeatmapId == -1)
		{
			if (lobby.PreviousBeatmapId != matchData.BeatmapId)
				lobby.Chat.SendBotMessage($"Selected: {matchData.MapEmbed()}");

			var beatmap = await beatmaps.GetBeatmapWithMD5(matchData.BeatmapMD5, -1);

			if (beatmap != null)
			{
				lobby.BeatmapId = beatmap.MapId;
				lobby.BeatmapName = beatmap.FullName();
				lobby.BeatmapMD5 = beatmap.MD5;
				lobby.Mode = host.Player!.Status.Mode.AsVanilla();
			}
			else
			{
				lobby.BeatmapId = matchData.BeatmapId;
				lobby.BeatmapName = matchData.BeatmapName;
				lobby.BeatmapMD5 = matchData.BeatmapMD5;
				lobby.Mode = matchData.Mode;
			}
		}

		if (lobby.Type != matchData.Type)
		{
			var newType = matchData.Type is LobbyType.HeadToHead or LobbyType.TagCoop
				? LobbyTeams.Neutral
				: LobbyTeams.Red;

			foreach (var slot in slots)
				if (slot.Player != null)
					slot.Team = newType;

			lobby.Type = matchData.Type;
		}

		if (lobby.WinCondition != matchData.WinCondition)
			lobby.WinCondition = matchData.WinCondition;
		
		lobby.Name = matchData.Name;
		
		lobby.EnqueueState();
	}
}