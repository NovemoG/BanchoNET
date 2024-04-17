using BanchoNET.Objects;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Services;

namespace BanchoNET.Utils;

public static class MultiplayerExtensions
{
	public static readonly int[] StartTimerAlerts = [
		60, 30, 15, 10, 5, 4, 3, 2, 1
	];
	
	public static string MPLinkEmbed(this MultiplayerLobby lobby)
	{
		return $"[https://osu.{AppSettings.Domain}/matches/{lobby.LobbyId} Multiplayer Link]";
	}

	public static string Url(this MultiplayerLobby lobby)
	{
		return $"osump://{lobby.Id}/{lobby.Password}";
	}

	public static string Embed(this MultiplayerLobby lobby)
	{
		return $"[{lobby.Url()} {lobby.Name}]";
	}

	public static string MapEmbed(this MultiplayerLobby lobby)
	{
		return $"https://osu.{AppSettings.Domain}/b/{lobby.BeatmapId} {lobby.BeatmapName}";
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
			if (slot.Status == expectedStatus)
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

	public static MultiplayerSlot GetHostSlot(this MultiplayerLobby lobby)
	{
		return lobby.Slots.First(s => s.Player?.Id == lobby.HostId);
	}

	public static MultiplayerSlot GetPlayerSlot(this MultiplayerLobby lobby, Player player)
	{
		return lobby.Slots.First(s => s.Player == player);
	}

	public static int GetPlayerSlotId(this MultiplayerLobby lobby, Player player)
	{
		for (int i = 0; i < lobby.Slots.Length; i++)
			if (lobby.Slots[i].Player == player)
				return i;

		return -1;
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

		using var matchStartPacket = new ServerPackets();
		matchStartPacket.MatchStart(lobby);
		lobby.Enqueue(matchStartPacket.GetContent(), noMapPlayerIds, false);
		lobby.EnqueueState();
	}

	public static void Enqueue(
		this MultiplayerLobby lobby,
		byte[] data,
		List<int>? immune = default,
		bool toLobby = true)
	{
		lobby.Chat.EnqueueToPlayers(data, immune);
		
		var lobbyChannel = BanchoSession.Instance.GetChannel("#lobby")!;
		if (toLobby && lobbyChannel.Players.Count > 0)
			lobbyChannel.EnqueueToPlayers(data);
	}

	public static void EnqueueState(this MultiplayerLobby lobby, bool toLobby = true)
	{
		using (var updatePacket = new ServerPackets())
		{
			updatePacket.UpdateMatch(lobby, true);
			lobby.Chat.EnqueueToPlayers(updatePacket.GetContent());
		}
		
		var lobbyChannel = BanchoSession.Instance.GetChannel("#lobby")!;
		if (toLobby && lobbyChannel.Players.Count > 0)
		{
			using var updatePacket = new ServerPackets();
			updatePacket.UpdateMatch(lobby, false);
			lobbyChannel.EnqueueToPlayers(updatePacket.GetContent());
		}
	}
}