using BanchoNET.Objects;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Services;

namespace BanchoNET.Utils;

public static class MultiplayerExtensions
{
	public static readonly int[] Alerts = [
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
		return $"[https://osu.{AppSettings.Domain}/b/{lobby.BeatmapId} {lobby.BeatmapName}]";
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
			if (slot.Status.HasStatus(expectedStatus))
				slot.Status = SlotStatus.NotReady;
	}

	public static void Reset(this MultiplayerSlot slot, SlotStatus newStatus)
	{
		slot.Player = null;
		slot.Status = newStatus;
		slot.Team = LobbyTeams.Neutral;
		slot.Mods = Mods.None;
		slot.Loaded = false;
		slot.Skipped = false;
	}

	public static MultiplayerSlot GetHostSlot(this MultiplayerLobby lobby)
	{
		Console.WriteLine($"host: {lobby.HostId}");
		foreach (var slot in lobby.Slots)
		{
			Console.WriteLine(slot.Player?.Id);
		}
		
		return lobby.Slots.First(s => s.Player?.Id == lobby.HostId);
	}

	public static void EnqueueState(this MultiplayerLobby lobby)
	{
		using (var updatePacket = new ServerPackets())
		{
			updatePacket.UpdateMatch(lobby, true);
			lobby.Chat.EnqueueToPlayers(updatePacket.GetContent());
		}
		
		var lobbyChannel = BanchoSession.Instance.GetChannel("#lobby")!;
		if (lobbyChannel.Players.Count > 0)
		{
			using var updatePacket = new ServerPackets();
			updatePacket.UpdateMatch(lobby, false);
			lobbyChannel.EnqueueToPlayers(updatePacket.GetContent());
		}
	}
}