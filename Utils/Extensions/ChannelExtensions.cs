using System.Collections.Immutable;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Services;

namespace BanchoNET.Utils.Extensions;

public static class ChannelExtensions
{
	private static readonly BanchoSession Session = BanchoSession.Instance;
	
	public static readonly ImmutableList<Channel> DefaultChannels = ImmutableList.Create(
		new Channel("#osu")
		{
			Description = "Main osu! chatroom",
			AutoJoin = true,
			Hidden = false,
			ReadOnly = false,
			Instance = false,
			ReadPrivileges = ClientPrivileges.Player,
			WritePrivileges = ClientPrivileges.Player,
		},
		new Channel("#lobby")
		{
			Description = "Multiplayer chatroom",
			AutoJoin = false,
			Hidden = false,
			ReadOnly = false,
			Instance = false,
			ReadPrivileges = ClientPrivileges.Player,
			WritePrivileges = ClientPrivileges.Player,
		},
		new Channel("#announce")
		{
			Description = "Chatroom for announcements about scores and maps",
			AutoJoin = false,
			Hidden = false,
			ReadOnly = false,
			Instance = false,
			ReadPrivileges = ClientPrivileges.Player,
			WritePrivileges = ClientPrivileges.Player,
		},
		new Channel("#staff")
		{
			Description = "osu! staff chatroom",
			AutoJoin = false,
			Hidden = true,
			ReadOnly = false,
			Instance = false,
			ReadPrivileges = ClientPrivileges.Owner,
			WritePrivileges = ClientPrivileges.Owner,
		}
	);
	
	public static bool PlayerInChannel(this Channel channel, Player player)
	{
		return channel.Players.Any(p => p.Id == player.Id);
	}
	
	public static bool CanPlayerRead(this Channel channel, Player player)
	{
		return player.ToBanchoPrivileges().CompareHighestPrivileges(channel.ReadPrivileges);
	}

	public static bool CanPlayerWrite(this Channel channel, Player player)
	{
		return player.ToBanchoPrivileges().CompareHighestPrivileges(channel.WritePrivileges);
	}

	public static void SendMessage(this Channel channel, Message message, bool toSelf = false)
	{
		var messageBytes = new ServerPacket()
			.SendMessage(new Message
			{
				Sender = message.Sender,
				Content = message.Content,
				Destination = channel.Name,
				SenderId = message.SenderId
			})
			.FinalizeAndGetContent();
		
		foreach (var player in channel.Players)
		{
			if (!player.BlockedByPlayer(message.SenderId) && (toSelf || player.Id != message.SenderId))
				player.Enqueue(messageBytes);
		}
	}

	public static void SendBotMessage(this Channel channel, string message)
	{
		var bot = Session.BanchoBot;

		if (message.Length >= 31979)
			message = $"message would have crashed games ({message.Length} characters).";
		
		channel.EnqueueToPlayers(new ServerPacket()
			.SendMessage(new Message
			{
				Sender = bot.Username,
				Content = message,
				Destination = channel.Name,
				SenderId = bot.Id
			})
			.FinalizeAndGetContent());
	}

	public static void EnqueueIfCanRead(this Channel channel, byte[] data)
	{
		foreach (var player in Session.Players)
			if (channel.CanPlayerRead(player))
				player.Enqueue(data);
	}

	public static void EnqueueToPlayers(this Channel channel, byte[] data, List<int>? immune = default)
	{
		immune ??= [];
		
		foreach (var player in channel.Players)
			if (!immune.Remove(player.Id))
				player.Enqueue(data);
	}
}