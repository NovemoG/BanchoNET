using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using Novelog;

namespace BanchoNET.Core.Utils.Extensions;

public static class MultiplayerExtensions
{
	extension(
		MultiplayerMatch match
	) {
		public string MPLinkEmbed()
		{
			return $"[https://osu.{AppSettings.Domain}/matches/{match.LobbyId} Multiplayer Link]";
		}

		public string Url()
		{
			//TODO apparently spaces are replaced with _ but I forgot to check other cases (spaces and _ at the same time)
			return $"osump://{match.Id}/{match.Password.Replace(' ', '_')}";
		}

		public string Embed()
		{
			return $"[{match.Url()} {match.Name}]";
		}

		public string MapEmbed()
		{
			return $"https://osu.{AppSettings.Domain}/b/{match.BeatmapId} {match.BeatmapName}";
		}

		public void ResetPlayersLoadedStatuses()
		{
			foreach (var slot in match.Slots)
			{
				slot.Loaded = false;
				slot.Skipped = false;
			}
		}

		public void UnreadyPlayers(
			SlotStatus expectedStatus = SlotStatus.Ready
		) {
			foreach (var slot in match.Slots)
				if ((slot.Status & expectedStatus) != 0)
					slot.Status = SlotStatus.NotReady;
		}

		public MultiplayerSlot GetHostSlot()
		{
			return match.Slots.First(s => s.Player?.Id == match.HostId);
		}

		public MultiplayerSlot? GetPlayerSlot(
			User player
		) {
			return match.Slots.FirstOrDefault(s => s.Player == player);
		}

		public MultiplayerSlot? GetPlayerSlot(
			string username
		) {
			return match.Slots.FirstOrDefault(s =>
				s.Player != null && s.Player.Username.Equals(username, StringComparison.CurrentCultureIgnoreCase));
		}

		public MultiplayerSlot? GetPlayerSlot(
			int playerId
		) {
			return match.Slots.FirstOrDefault(s => s.Player != null && s.Player.Id == playerId);
		}

		public int GetPlayerSlotId(
			User player
		) {
			for (int i = 0; i < match.Slots.Length; i++)
				if (match.Slots[i].Player == player)
					return i;

			return -1;
		}

		public void ReadyAllPlayers()
		{
			foreach (var slot in match.Slots)
				if (slot.Status == SlotStatus.NotReady)
					slot.Status = SlotStatus.Ready;
		}

		public bool IsEmpty()
		{
			return match.Slots.All(s => s.Player == null) && match.TourneyClients.Count == 0;
		}
	}

	extension(
		MultiplayerSlot slot
	) {
		public void Reset(
			SlotStatus newStatus = SlotStatus.Open
		) {
			slot.Player = null;
			slot.Status = newStatus;
			slot.Team = LobbyTeams.Neutral;
			slot.Mods = Mods.None;
			slot.Loaded = false;
			slot.Skipped = false;
		}

		public void CopyStatusFrom(
			MultiplayerSlot other
		) {
			slot.Player = other.Player;
			slot.Status = other.Status;
			slot.Team = other.Team;
			slot.Mods = other.Mods;
		}

		public MultiplayerSlot Copy()
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
	}
	
	public static void InviteToLobby(User player, User? target)
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
}