using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using Novelog;

namespace BanchoNET.Core.Utils.Extensions;

public static class MultiplayerExtensions
{
	public static string MPLinkEmbed(this MultiplayerMatch match)
	{
		return $"[https://osu.{AppSettings.Domain}/matches/{match.LobbyId} Multiplayer Link]";
	}

	public static string Url(this MultiplayerMatch match)
	{
		//TODO apparently spaces are replaced with _ but I forgot to check other cases (spaces and _ at the same time)
		return $"osump://{match.Id}/{match.Password.Replace(' ', '_')}";
	}

	public static string Embed(this MultiplayerMatch match)
	{
		return $"[{match.Url()} {match.Name}]";
	}

	public static string MapEmbed(this MultiplayerMatch match)
	{
		return $"https://osu.{AppSettings.Domain}/b/{match.BeatmapId} {match.BeatmapName}";
	}

	public static void CreateLobby(MultiplayerMatch match, Player player, int lobbyId)
	{
		var matchChannel = new Channel($"#multi_{match.Id}")
		{
			Description = "This multiplayer's channel.",
			AutoJoin = false,
			Instance = true
		};

		match.Id = Session.GetFreeMatchId();
		match.LobbyId = lobbyId;
		match.Chat = matchChannel; 
		match.Refs.Add(player.Id);
		
		Session.InsertLobby(match);
		Session.InsertChannel(matchChannel);

		player.JoinMatch(match, match.Password);
		Logger.Shared.LogDebug($"{player.Username} created a match with ID {match.LobbyId}, in-game ID: {match.Id}.", nameof(MultiplayerExtensions));
	}
	
	public static void InviteToLobby(Player player, Player? target)
	{
		if (target == null) return;
		if (target.IsBot)
		{
			player.SendBotMessage("I'm too busy right now! Maybe later \ud83d\udc7c");
			return;
		}
		
		target.Enqueue(new ServerPackets()
			.MatchInvite(player, target.Username)
			.FinalizeAndGetContent());
		
		Logger.Shared.LogDebug($"{player.Username} invited {target.Username} to their match.", nameof(MultiplayerExtensions));
	}
	
	public static void ResetPlayersLoadedStatuses(this MultiplayerMatch match)
	{
		foreach (var slot in match.Slots)
		{
			slot.Loaded = false;
			slot.Skipped = false;
		}
	}

	public static void UnreadyPlayers(this MultiplayerMatch match, SlotStatus expectedStatus = SlotStatus.Ready)
	{
		foreach (var slot in match.Slots)
			if ((slot.Status & expectedStatus) != 0)
				slot.Status = SlotStatus.NotReady;
	}

	public static void Reset(this MultiplayerSlot slot, SlotStatus newStatus = SlotStatus.Open)
	{
		slot.Player = null;
		slot.Status = newStatus;
		slot.Team = LobbyTeams.Neutral;
		slot.Mods = Mods.None;
		slot.Loaded = false;
		slot.Skipped = false;
	}

	public static void CopyStatusFrom(this MultiplayerSlot slot, MultiplayerSlot other)
	{
		slot.Player = other.Player;
		slot.Status = other.Status;
		slot.Team = other.Team;
		slot.Mods = other.Mods;
	}
	
	public static MultiplayerSlot Copy(this MultiplayerSlot slot)
	{
		return new MultiplayerSlot
		{
			Player = slot.Player,
			Status = slot.Status,
			Team = slot.Team,
			Mods = slot.Mods,
			Loaded = slot.Loaded,
			Skipped = slot.Skipped
		};
	}

	public static MultiplayerSlot GetHostSlot(this MultiplayerMatch match)
	{
		return match.Slots.First(s => s.Player?.Id == match.HostId);
	}
	
	public static MultiplayerSlot? GetPlayerSlot(this MultiplayerMatch match, Player player)
	{
		return match.Slots.FirstOrDefault(s => s.Player == player);
	}
	
	public static MultiplayerSlot? GetPlayerSlot(this MultiplayerMatch match, string username)
	{
		return match.Slots.FirstOrDefault(s =>
			s.Player != null && s.Player.Username.Equals(username, StringComparison.CurrentCultureIgnoreCase));
	}
	
	public static MultiplayerSlot? GetPlayerSlot(this MultiplayerMatch match, int playerId)
	{
		return match.Slots.FirstOrDefault(s => s.Player != null && s.Player.Id == playerId);
	}

	public static int GetPlayerSlotId(this MultiplayerMatch match, Player player)
	{
		for (int i = 0; i < match.Slots.Length; i++)
			if (match.Slots[i].Player == player)
				return i;

		return -1;
	}
	
	public static void ReadyAllPlayers(this MultiplayerMatch match)
	{
		foreach (var slot in match.Slots)
			if (slot.Status == SlotStatus.NotReady)
				slot.Status = SlotStatus.Ready;
	}

	public static void Start(this MultiplayerMatch match)
	{
		var noMapPlayerIds = new List<int>();

		foreach (var slot in match.Slots)
		{
			if (slot.Player == null) continue;

			if (slot.Status != SlotStatus.NoMap)
				slot.Status = SlotStatus.Playing;
			else
				noMapPlayerIds.Add(slot.Player.Id);
		}

		match.InProgress = true;
		
		match.Enqueue(new ServerPackets().MatchStart(match).FinalizeAndGetContent(),
			noMapPlayerIds,
			false);
		match.EnqueueState();
	}
	
	public static void End(this MultiplayerMatch match)
	{
		match.InProgress = false;
		match.ResetPlayersLoadedStatuses();
		
		match.UnreadyPlayers(SlotStatus.Playing | SlotStatus.Ready);
		
		match.Enqueue(new ServerPackets()
			.MatchAbort()
			.FinalizeAndGetContent());
		match.EnqueueState();
	}

	public static void EnqueueDispose(this MultiplayerMatch match)
	{
		var data = new ServerPackets()
			.DisposeMatch(match)
			.FinalizeAndGetContent();
		
		foreach (var player in Session.PlayersInLobby)
			player.Enqueue(data);
	}

	public static bool IsEmpty(this MultiplayerMatch match)
	{
		return match.Slots.All(s => s.Player == null) && match.TourneyClients.Count == 0;
	}

	public static void Enqueue(
		this MultiplayerMatch match,
		byte[] data,
		List<int>? immune = default,
		bool toLobby = true)
	{
		match.Chat.EnqueueToPlayers(data, immune);
		
		if (!toLobby) return;
		
		foreach (var player in Session.PlayersInLobby)
			player.Enqueue(data);
	}

	public static void EnqueueState(this MultiplayerMatch match, bool toLobby = true)
	{
		match.Chat.EnqueueToPlayers(new ServerPackets()
			.UpdateMatch(match, true)
			.FinalizeAndGetContent());

		if (!toLobby) return;
		
		var data = new ServerPackets()
			.UpdateMatch(match, false)
			.FinalizeAndGetContent();
		
		foreach (var player in Session.PlayersInLobby)
			player.Enqueue(data);
	}
}