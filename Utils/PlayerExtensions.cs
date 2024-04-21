using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Services;

namespace BanchoNET.Utils;

public static class PlayerExtensions
{ 
	private static readonly BanchoSession Session = BanchoSession.Instance;
	private static readonly Dictionary<Privileges, ClientPrivileges> ClientPrivilegesMap = new()
	{
		{ Privileges.Unrestricted, ClientPrivileges.Player },
		{ Privileges.Supporter, ClientPrivileges.Supporter },
		{ Privileges.Moderator, ClientPrivileges.Moderator },
		{ Privileges.Administrator, ClientPrivileges.Developer },
		{ Privileges.Developer, ClientPrivileges.Owner },
	};
	
	public static ClientPrivileges ToBanchoPrivileges(this Player player)
	{
		var retPriv = 0;
		var privs = (int)player.Privileges;
		
		foreach (var priv in Enum.GetValues<Privileges>())
		{
			if ((privs & (int)priv) == (int)priv && ClientPrivilegesMap.TryGetValue(priv, out var value)) 
				retPriv |= (int)value;
		}

		return (ClientPrivileges)retPriv;
	}
	
	public static bool CanUseCommand(this Player player, Privileges requiredPrivileges)
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
		using var messagePacket = new ServerPackets();
		messagePacket.SendMessage(new Message
		{
			Sender = source.Username,
			Content = message,
			Destination = channel != null ? channel.Name : player.Username,
			SenderId = source.Id
		});
		player.Enqueue(messagePacket.GetContent());
	}

	public static void SendBotMessage(this Player player, string message)
	{
		var bot = Session.BanchoBot;
		
		using var messagePacket = new ServerPackets();
		messagePacket.SendMessage(new Message
		{
			Sender = bot.Username,
			Content = message,
			Destination = player.Username,
			SenderId = bot.Id
		});
		player.Enqueue(messagePacket.GetContent());
	}

	public static void JoinMatch(this Player player, MultiplayerLobby lobby, string password)
	{
		if (player.InMatch)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			player.Enqueue(joinFailPacket.GetContent());
			
			Console.WriteLine($"[PlayerExtensions] {player.Username} tried to join multiple matches.");
			return;
		}
		
		//TODO tourney clients

		MultiplayerSlot? slot;
		if (lobby.HostId != player.Id)
		{
			if (password != lobby.Password && !player.Privileges.HasFlag(Privileges.Staff))
			{
				using var joinFailPacket = new ServerPackets();
				joinFailPacket.MatchJoinFail();
				player.Enqueue(joinFailPacket.GetContent());

				Console.WriteLine($"[PlayerExtensions] {player.Username} tried to join {lobby.LobbyId} with incorrect password.");
				return;
			}

			slot = lobby.Slots.FirstOrDefault(s => s.Status == SlotStatus.Open);
			if (slot == null)
			{
				using var joinFailPacket = new ServerPackets();
				joinFailPacket.MatchJoinFail();
				player.Enqueue(joinFailPacket.GetContent());
				return;
			}
		}
		else
			slot = lobby.Slots[0];

		if (!player.JoinChannel(lobby.Chat))
		{
			Console.WriteLine($"[PlayerExtensions] {player.Username} failed to join {lobby.Chat.IdName}");
			return;
		}

		var lobbyChannel = Session.GetChannel("#lobby")!;
		if (player.Channels.Contains(lobbyChannel)) 
			player.LeaveChannel(lobbyChannel);

		if (lobby.Type is LobbyType.TeamVS or LobbyType.TagTeamVS)
			slot.Team = LobbyTeams.Red;

		slot.Status = SlotStatus.NotReady;
		slot.Player = player;
		player.Lobby = lobby;
		
		using var joinSuccessPacket = new ServerPackets();
		joinSuccessPacket.MatchJoinSuccess(lobby);
		player.Enqueue(joinSuccessPacket.GetContent());
		lobby.EnqueueState();
	}
	
	public static void LeaveMatch(this Player player)
	{
		if (!player.InMatch)
		{
			Console.WriteLine($"[PlayerExtensions] {player.Username} tried to leave a match without being in one.");
			return;
		}

		var lobby = player.Lobby!;
		var slot = lobby.Slots.First(s => s.Player == player);
		
		slot.Reset(slot.Status == SlotStatus.Locked ? SlotStatus.Locked : SlotStatus.Open);
		player.LeaveChannel(lobby.Chat);

		if (lobby.Slots.All(s => s.Player == null) /*TODO check for tourney clients*/)
		{
			Console.WriteLine($"[PlayerExtensions] Match \"{lobby.Name}\" is empty, removing.");

			lobby.Timer?.Stop();

			Session.RemoveLobby(lobby);

			var lobbyChannel = Session.GetChannel("#lobby")!;
			using var matchDisposePacket = new ServerPackets();
			matchDisposePacket.DisposeMatch(lobby);
			lobbyChannel.EnqueueToPlayers(matchDisposePacket.GetContent());
		}
		else
		{
			if (lobby.HostId == player.Id)
			{
				var firstOccupiedSlot = lobby.Slots.First(s => s.Player != null);
				lobby.HostId = firstOccupiedSlot.Player!.Id;
				using var matchTransferPacket = new ServerPackets();
				matchTransferPacket.MatchTransferHost();
				firstOccupiedSlot.Player.Enqueue(matchTransferPacket.GetContent());
			}

			if (lobby.Refs.Remove(player.Id))
				lobby.Chat.SendBotMessage($"{player.Username} removed from match referees.");
			
			lobby.EnqueueState();
		}

		player.Lobby = null;
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
			Console.WriteLine($"[PlayerExtensions] {target.Username} failed to join {channelName}");
			return;
		}

		using var joinedPacket = new ServerPackets();
		joinedPacket.FellowSpectatorJoined(target.Id);
		var bytes = joinedPacket.GetContent();
		
		foreach (var spectator in host.Spectators)
		{
			spectator.Enqueue(bytes);

			using var fellowSpectatorPacket = new ServerPackets();
			fellowSpectatorPacket.FellowSpectatorJoined(spectator.Id);
			target.Enqueue(fellowSpectatorPacket.GetContent());
		}
		
		using var spectatorJoinedPacket = new ServerPackets();
		spectatorJoinedPacket.SpectatorJoined(target.Id);
		host.Enqueue(spectatorJoinedPacket.GetContent());
		
		host.Spectators.Add(target);
		target.Spectating = host;
		
		Console.WriteLine($"{target.Username} is now spectating {host.Username}");
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
		
		using var spectatorLeftPacket = new ServerPackets();
		spectatorLeftPacket.SpectatorLeft(target.Id);
		host.Enqueue(spectatorLeftPacket.GetContent());
		
		Console.WriteLine($"{target.Username} is no longer spectating {host.Username}");
	}

	public static bool BlockedByPlayer(this Player player, int targetId)
	{
		return player.Blocked.Contains(targetId);
	}

	public static bool JoinChannel(this Player player, Channel channel)
	{
		if (channel.PlayerInChannel(player) ||
		    !channel.CanPlayerRead(player) ||
		    (channel.IdName == "#lobby" && !player.InLobby))
		{
			return false;
		}

		player.AddToChannel(channel);

		using var channelJoinPacket = new ServerPackets();
		channelJoinPacket.ChannelJoin(channel.Name);
		player.Enqueue(channelJoinPacket.GetContent());

		using var channelInfoPacket = new ServerPackets();
		channelInfoPacket.ChannelInfo(channel);
		var bytes = channelInfoPacket.GetContent();

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
			Console.WriteLine($"[PlayerExtensions] {player.Username} tried to leave {channel.IdName} without being in it");
			return;
		}
		
		player.RemoveFromChannel(channel);

		if (kick)
		{
			using var kickPacket = new ServerPackets();
			kickPacket.ChannelKick(channel.Name);
			player.Enqueue(kickPacket.GetContent());
		}
		
		using var channelInfoPacket = new ServerPackets();
		channelInfoPacket.ChannelInfo(channel);
		var bytes = channelInfoPacket.GetContent();

		if (channel.Instance)
			foreach (var user in channel.Players)
				user.Enqueue(bytes);
		else
			foreach (var user in Session.Players.Where(channel.CanPlayerRead))
				user.Enqueue(bytes);
		
		Console.WriteLine($"[PlayerExtensions] {player.Username} left {channel.IdName}");
	}

	private static void AddToChannel(this Player player, Channel channel)
	{
		channel.Players.Add(player);
		player.Channels.Add(channel);
	}

	private static void RemoveFromChannel(this Player player, Channel channel)
	{
		channel.Players.Remove(player);
		player.Channels.Remove(channel);
	}
}