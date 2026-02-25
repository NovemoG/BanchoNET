using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Users;
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
	
	public static ClientPrivileges ToBanchoPrivileges(this User player)
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
	
	public static bool CanUseCommand(this User player, PlayerPrivileges requiredPrivileges)
	{
		return player.Privileges.HasAnyPrivilege(requiredPrivileges) &&
		       player.Privileges.CompareHighestPrivileges(requiredPrivileges);
	}

	/// <summary>
	/// Sends a message directly to this player. If channel is provided, it will be sent to that channel instead.
	/// </summary>
	/// <param name="player"></param>
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