using BanchoNET.Objects;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Services;

namespace BanchoNET.Utils;

public static class MultiplayerExtensions
{
	private static readonly BanchoSession Session = BanchoSession.Instance;
	
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

	public static void CreateLobby(MultiplayerLobby lobby, Player player, int lobbyId)
	{
		var matchChannel = new Channel($"#multi_{lobby.Id}")
		{
			Description = "This multiplayer's channel.",
			AutoJoin = false,
			Instance = true
		};

		lobby.LobbyId = lobbyId;
		lobby.Chat = matchChannel; 
		lobby.Refs.Add(player.Id);
		
		Session.InsertLobby(lobby);
		Session.InsertChannel(matchChannel);

		player.JoinMatch(lobby, lobby.Password);
		
		matchChannel.SendBotMessage($"Match created by {player.Username} {lobby.MPLinkEmbed()}");
		Console.WriteLine($"[CreateMatch] {player.Username} created a match with ID {lobby.LobbyId}, in-game ID: {lobby.Id}.");
	}
	
	public static void InviteToLobby(Player player, Player? target)
	{
		if (target == null) return;
		if (target.IsBot)
		{
			player.SendBotMessage("I'm too busy right now! Maybe later \ud83d\udc7c");
			return;
		}

		using var invitePacket = new ServerPackets();
		invitePacket.MatchInvite(player, target.Username);
		target.Enqueue(invitePacket.GetContent());
		
		Console.WriteLine($"[MatchInvite] {player.Username} invited {target.Username} to their match.");
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

		using var matchStartPacket = new ServerPackets();
		matchStartPacket.MatchStart(lobby);
		lobby.Enqueue(matchStartPacket.GetContent(), noMapPlayerIds, false);
		lobby.EnqueueState();
	}
	
	public static void End(this MultiplayerLobby lobby)
	{
		lobby.InProgress = false;
		lobby.ResetPlayersLoadedStatuses();
		
		lobby.UnreadyPlayers(SlotStatus.Playing | SlotStatus.Ready);
		//lobby.UnreadyPlayers();
		
		using var matchEndPacket = new ServerPackets();
		matchEndPacket.MatchAbort();
		lobby.Enqueue(matchEndPacket.GetContent());
		lobby.EnqueueState();
	}

	public static void Enqueue(
		this MultiplayerLobby lobby,
		byte[] data,
		List<int>? immune = default,
		bool toLobby = true)
	{
		lobby.Chat.EnqueueToPlayers(data, immune);
		
		var lobbyChannel = Session.GetChannel("#lobby")!;
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
		
		var lobbyChannel = Session.GetChannel("#lobby")!;
		if (toLobby && lobbyChannel.Players.Count > 0)
		{
			using var updatePacket = new ServerPackets();
			updatePacket.UpdateMatch(lobby, false);
			lobbyChannel.EnqueueToPlayers(updatePacket.GetContent());
		}
	}
}