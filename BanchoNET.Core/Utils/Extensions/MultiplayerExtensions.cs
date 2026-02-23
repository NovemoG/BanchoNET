using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using Novelog;

namespace BanchoNET.Core.Utils.Extensions;

public static class MultiplayerExtensions
{
	public static string MPLinkEmbed(this MultiplayerLobby lobby)
	{
		return $"[https://osu.{AppSettings.Domain}/matches/{lobby.LobbyId} Multiplayer Link]";
	}

	public static string Url(this MultiplayerLobby lobby)
	{
		//TODO apparently spaces are replaced with _ but I forgot to check other cases (spaces and _ at the same time)
		return $"osump://{lobby.Id}/{lobby.Password.Replace(' ', '_')}";
	}

	public static string Embed(this MultiplayerLobby lobby)
	{
		return $"[{lobby.Url()} {lobby.Name}]";
	}

	public static string MapEmbed(this MultiplayerLobby lobby)
	{
		return $"https://osu.{AppSettings.Domain}/b/{lobby.BeatmapId} {lobby.BeatmapName}";
	}

	public static void CreateLobby(MultiplayerLobby lobby, Player player, int lobbyId)
	{
		var matchChannel = new Channel($"#multi_{lobby.Id}")
		{
			Description = "This multiplayer's channel.",
			AutoJoin = false,
			Instance = true
		};

		lobby.Id = Session.GetFreeMatchId();
		lobby.LobbyId = lobbyId;
		lobby.Chat = matchChannel; 
		lobby.Refs.Add(player.Id);
		
		Session.InsertLobby(lobby);
		Session.InsertChannel(matchChannel);

		player.JoinMatch(lobby, lobby.Password);
		Logger.Shared.LogDebug($"{player.Username} created a match with ID {lobby.LobbyId}, in-game ID: {lobby.Id}.", nameof(MultiplayerExtensions));
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
	
	public static void ResetPlayersLoadedStatuses(this MultiplayerLobby lobby)
	{
		foreach (var slot in lobby.Slots)
		{
			slot.Loaded = false;
			slot.Skipped = false;
		}
	}

	public static void UnreadyPlayers(this MultiplayerLobby lobby, SlotStatus expectedStatus = SlotStatus.Ready)
	{
		foreach (var slot in lobby.Slots)
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

	public static MultiplayerSlot GetHostSlot(this MultiplayerLobby lobby)
	{
		return lobby.Slots.First(s => s.Player?.Id == lobby.HostId);
	}
	
	public static MultiplayerSlot? GetPlayerSlot(this MultiplayerLobby lobby, Player player)
	{
		return lobby.Slots.FirstOrDefault(s => s.Player == player);
	}
	
	public static MultiplayerSlot? GetPlayerSlot(this MultiplayerLobby lobby, string username)
	{
		return lobby.Slots.FirstOrDefault(s =>
			s.Player != null && s.Player.Username.Equals(username, StringComparison.CurrentCultureIgnoreCase));
	}
	
	public static MultiplayerSlot? GetPlayerSlot(this MultiplayerLobby lobby, int playerId)
	{
		return lobby.Slots.FirstOrDefault(s => s.Player != null && s.Player.Id == playerId);
	}

	public static int GetPlayerSlotId(this MultiplayerLobby lobby, Player player)
	{
		for (int i = 0; i < lobby.Slots.Length; i++)
			if (lobby.Slots[i].Player == player)
				return i;

		return -1;
	}
	
	public static void ReadyAllPlayers(this MultiplayerLobby lobby)
	{
		foreach (var slot in lobby.Slots)
			if (slot.Status == SlotStatus.NotReady)
				slot.Status = SlotStatus.Ready;
	}

	public static void Start(this MultiplayerLobby lobby)
	{
		var noMapPlayerIds = new List<int>();

		foreach (var slot in lobby.Slots)
		{
			if (slot.Player == null) continue;

			if (slot.Status != SlotStatus.NoMap)
				slot.Status = SlotStatus.Playing;
			else
				noMapPlayerIds.Add(slot.Player.Id);
		}

		lobby.InProgress = true;
		
		lobby.Enqueue(new ServerPackets().MatchStart(lobby).FinalizeAndGetContent(),
			noMapPlayerIds,
			false);
		lobby.EnqueueState();
	}
	
	public static void End(this MultiplayerLobby lobby)
	{
		lobby.InProgress = false;
		lobby.ResetPlayersLoadedStatuses();
		
		lobby.UnreadyPlayers(SlotStatus.Playing | SlotStatus.Ready);
		
		lobby.Enqueue(new ServerPackets()
			.MatchAbort()
			.FinalizeAndGetContent());
		lobby.EnqueueState();
	}

	public static void EnqueueDispose(this MultiplayerLobby lobby)
	{
		var data = new ServerPackets()
			.DisposeMatch(lobby)
			.FinalizeAndGetContent();
		
		foreach (var player in Session.PlayersInLobby)
			player.Enqueue(data);
	}

	public static bool IsEmpty(this MultiplayerLobby lobby)
	{
		return lobby.Slots.All(s => s.Player == null) && lobby.TourneyClients.Count == 0;
	}

	public static void Enqueue(
		this MultiplayerLobby lobby,
		byte[] data,
		List<int>? immune = default,
		bool toLobby = true)
	{
		lobby.Chat.EnqueueToPlayers(data, immune);
		
		if (!toLobby) return;
		
		foreach (var player in Session.PlayersInLobby)
			player.Enqueue(data);
	}

	public static void EnqueueState(this MultiplayerLobby lobby, bool toLobby = true)
	{
		lobby.Chat.EnqueueToPlayers(new ServerPackets()
			.UpdateMatch(lobby, true)
			.FinalizeAndGetContent());

		if (!toLobby) return;
		
		var data = new ServerPackets()
			.UpdateMatch(lobby, false)
			.FinalizeAndGetContent();
		
		foreach (var player in Session.PlayersInLobby)
			player.Enqueue(data);
	}
}