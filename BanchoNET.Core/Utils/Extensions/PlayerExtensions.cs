using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Packets;
using Novelog;

namespace BanchoNET.Core.Utils.Extensions;

public static class PlayerExtensions
{
	private static readonly Dictionary<PlayerPrivileges, ClientPrivileges> ClientPrivilegesMap = new()
	{
		{ PlayerPrivileges.Unrestricted, ClientPrivileges.Player },
		{ PlayerPrivileges.Supporter, ClientPrivileges.Supporter },
		{ PlayerPrivileges.Moderator, ClientPrivileges.Moderator },
		{ PlayerPrivileges.Administrator, ClientPrivileges.Developer },
		{ PlayerPrivileges.Developer, ClientPrivileges.Owner },
	};
	
	public static ClientPrivileges ToBanchoPrivileges(this Player player)
	{
		var retPriv = 0;
		var privs = (int)player.Privileges;
		
		foreach (var priv in Enum.GetValues<PlayerPrivileges>())
		{
			if ((privs & (int)priv) == (int)priv && ClientPrivilegesMap.TryGetValue(priv, out var value)) 
				retPriv |= (int)value;
		}

		return (ClientPrivileges)retPriv;
	}
	
	public static bool CanUseCommand(this Player player, PlayerPrivileges requiredPrivileges)
	{
		return player.Privileges.HasAnyPrivilege(requiredPrivileges) &&
		       player.Privileges.CompareHighestPrivileges(requiredPrivileges);
	}

	/// <summary>
	/// Sends a message directly to this player. If channel is provided, it will be sent to that channel instead.
	/// </summary>
	/// <param name="message">Message content</param>
	/// <param name="source">Player that sent message</param>
	/// <param name="channel">(optional) channel that this message should be sent to instead of player</param>
	public static void SendMessage(this Player player, string message, Player source, Channel? channel = null)
	{
		player.Enqueue(new ServerPackets()
			.SendMessage(new Message
			{
				Sender = source.Username,
				Content = message,
				Destination = channel != null ? channel.Name : player.Username,
				SenderId = source.Id
			}).FinalizeAndGetContent());
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="player"></param>
	/// <param name="message"></param>
	/// <param name="destination">(optional) </param>
	public static void SendBotMessage(this Player player, string message, string destination = "")
	{
		player.Enqueue(new ServerPackets()
			.SendMessage(new Message
			{
				Sender = BanchoBot.Username,
				Content = message,
				Destination = string.IsNullOrEmpty(destination) ? player.Username : destination,
				SenderId = BanchoBot.Id
			}).FinalizeAndGetContent());
	}

	public static bool JoinMatch(this Player player, MultiplayerLobby lobby, string password)
	{
		if (player.InMatch)
		{
			player.Enqueue(MatchJoinFailData());
			
			Logger.Shared.LogDebug($"{player.Username} tried to join multiple matches.", nameof(PlayerExtensions));
			return false;
		}

		if (lobby.TourneyClients.Contains(player.Id))
		{
			player.Enqueue(MatchJoinFailData());
			return false;
		}

		MultiplayerSlot? slot;
		if (lobby.HostId != player.Id)
		{
			if (password != lobby.Password && !player.Privileges.HasFlag(PlayerPrivileges.Staff))
			{
				player.Enqueue(MatchJoinFailData());

				Logger.Shared.LogDebug($"{player.Username} tried to join {lobby.LobbyId} with incorrect password.", nameof(PlayerExtensions));
				return false;
			}

			slot = lobby.Slots.FirstOrDefault(s => s.Status == SlotStatus.Open);
			if (slot == null)
			{
				player.Enqueue(MatchJoinFailData());
				return false;
			}
		}
		else slot = lobby.Slots[0];

		if (!player.JoinChannel(lobby.Chat))
		{
			Logger.Shared.LogDebug($"{player.Username} failed to join {lobby.Chat.IdName}", nameof(PlayerExtensions));
			return false;
		}
		
		player.LeaveChannel(Session.LobbyChannel);

		if (lobby.Type is LobbyType.TeamVS or LobbyType.TagTeamVS)
			slot.Team = LobbyTeams.Red;

		slot.Status = SlotStatus.NotReady;
		slot.Player = player;
		player.Lobby = lobby;
		
		player.Enqueue(new ServerPackets()
			.MatchJoinSuccess(lobby)
			.FinalizeAndGetContent());
		lobby.EnqueueState();
		
		player.SendBotMessage($"Match created by {player.Username} {lobby.MPLinkEmbed()}", "#multiplayer");
		return true;
	}

	private static byte[] MatchJoinFailData()
	{
		return new ServerPackets()
			.MatchJoinFail()
			.FinalizeAndGetContent();
	}
	
	public static bool LeaveMatch(this Player player)
	{
		if (!player.InMatch)
		{
			Logger.Shared.LogDebug($"{player.Username} tried to leave a match without being in one.", nameof(PlayerExtensions));
			return false;
		}

		var lobby = player.Lobby!;
		var slot = lobby.Slots.First(s => s.Player == player);
		
		slot.Reset(slot.Status == SlotStatus.Locked ? SlotStatus.Locked : SlotStatus.Open);
		player.LeaveChannel(lobby.Chat);

		if (lobby.IsEmpty())
		{
			Logger.Shared.LogDebug($"Match \"{lobby.Name}\" is empty, removing.", nameof(PlayerExtensions));

			lobby.Timer?.Stop();
			Session.RemoveLobby(lobby);
			lobby.EnqueueDispose();
		}
		else
		{
			if (lobby.HostId == player.Id)
			{
				var firstOccupiedSlot = lobby.Slots.First(s => s.Player != null);
				lobby.HostId = firstOccupiedSlot.Player!.Id;
				firstOccupiedSlot.Player.Enqueue(new ServerPackets()
					.MatchTransferHost()
					.FinalizeAndGetContent());
			}

			if (lobby.CreatorId != player.Id && lobby.Refs.Remove(player.Id))
				lobby.Chat.SendBotMessage($"Removed {player.Username} from match referees.");
			
			lobby.EnqueueState();
		}

		player.Lobby = null;
		return true;
	}

	public static void LeaveMatchToLobby(this Player player)
	{
		player.JoinLobby();
		player.JoinChannel(Session.LobbyChannel);
		player.LeaveMatch();
	}

	public static void AddSpectator(this Player host, Player target)
	{
		var channelName = $"#s_{host.Id}";
		var spectatorChannel = Session.GetChannel(channelName, true);

		if (spectatorChannel == null)
		{
			spectatorChannel = new Channel(channelName)
			{
				Description = $"{host.Username}'s spectator channel",
				AutoJoin = false,
				Instance = true
			};

			host.JoinChannel(spectatorChannel);
			Session.InsertChannel(spectatorChannel, true);
		}

		if (!target.JoinChannel(spectatorChannel))
		{
			Logger.Shared.LogDebug($"{target.Username} failed to join {channelName}", nameof(PlayerExtensions));
			return;
		}
		
		var bytes = new ServerPackets()
			.FellowSpectatorJoined(target.Id)
			.FinalizeAndGetContent();
		
		foreach (var spectator in host.Spectators)
		{
			spectator.Enqueue(bytes);
			
			target.Enqueue(new ServerPackets()
				.FellowSpectatorJoined(spectator.Id)
				.FinalizeAndGetContent());
		}
		
		host.Enqueue(new ServerPackets()
			.SpectatorJoined(target.Id)
			.FinalizeAndGetContent());
		
		host.Spectators.Add(target);
		target.Spectating = host;
		
		Logger.Shared.LogDebug($"{target.Username} is now spectating {host.Username}", nameof(PlayerExtensions));
	}
	
	public static void RemoveSpectator(this Player host, Player target)
	{
		host.Spectators.Remove(target);
		target.Spectating = null;

		var spectatorChannel = Session.GetChannel(name: $"#s_{host.Id}", true)!;
		target.LeaveChannel(spectatorChannel);

		if (host.Spectators.Count == 0)
			host.LeaveChannel(spectatorChannel);
		else
		{
			using var returnPacket = new ServerPackets();
			returnPacket.ChannelInfo(spectatorChannel);
			
			target.Enqueue(returnPacket.GetContent());
			
			returnPacket.FellowSpectatorLeft(target.Id);

			var bytes = returnPacket.GetContent();
			foreach (var spectator in host.Spectators)
				spectator.Enqueue(bytes);
		}
		
		host.Enqueue(new ServerPackets()
			.SpectatorLeft(target.Id)
			.FinalizeAndGetContent());
		
		Logger.Shared.LogDebug($"{target.Username} is no longer spectating {host.Username}", nameof(PlayerExtensions));
	}

	public static bool BlockedByPlayer(this Player player, int targetId)
	{
		return player.Blocked.Contains(targetId);
	}

	public static void JoinLobby(this Player player)
	{
		Session.JoinLobby(player);

		foreach (var lobby in Session.Lobbies)
		{
			player.Enqueue(new ServerPackets()
				.NewMatch(lobby)
				.FinalizeAndGetContent());
		}
	}

	public static bool JoinChannel(this Player player, Channel channel)
	{
		if (channel.PlayerInChannel(player) ||
		    !channel.CanPlayerRead(player) ||
		    (channel.IdName == "#lobby" && !player.InLobby))
		{
			return false;
		}

		channel.AddPlayer(player);
		player.Channels.Add(channel);
		
		player.Enqueue(new ServerPackets()
			.ChannelJoin(channel.Name)
			.FinalizeAndGetContent());
		
		var bytes = new ServerPackets()
			.ChannelInfo(channel)
			.FinalizeAndGetContent();
		
		if (channel.Instance)
			foreach (var user in channel.Players)
				user.Enqueue(bytes);
		else
			foreach (var user in Session.Players.Where(channel.CanPlayerRead))
				user.Enqueue(bytes);
		
		return true;
	}
	
	public static void LeaveChannel(this Player player, Channel channel, bool kick = true)
	{
		if (!channel.PlayerInChannel(player))
		{
			Logger.Shared.LogDebug($"{player.Username} tried to leave {channel.IdName} without being in it " +
			                       $"(most of the times it's a false positive).", nameof(PlayerExtensions));
			return;
		}
		
		channel.RemovePlayer(player);
		player.Channels.Remove(channel);

		if (kick)
		{
			player.Enqueue(new ServerPackets()
				.ChannelKick(channel.Name)
				.FinalizeAndGetContent());
		}
		
		var bytes = new ServerPackets()
			.ChannelInfo(channel)
			.FinalizeAndGetContent();

		if (channel.Instance)
			foreach (var user in channel.Players)
				user.Enqueue(bytes);
		else
			foreach (var user in Session.Players.Where(channel.CanPlayerRead))
				user.Enqueue(bytes);
		
		Logger.Shared.LogDebug($"{player.Username} left {channel.IdName}", nameof(PlayerExtensions));
	}
}