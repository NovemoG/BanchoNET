using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Services;

namespace BanchoNET.Utils;

public static class ChannelExtensions
{
	private static readonly BanchoSession Session = BanchoSession.Instance;
	
	public static bool PlayerInChannel(this Channel channel, Player player)
	{
		return channel.Players.Any(p => p.Id == player.Id);
	}
	
	public static bool CanPlayerRead(this Channel channel, Player player)
	{
		return player.ToBanchoPrivileges().HasPrivilege(channel.ReadPrivileges);
	}

	public static bool CanPlayerWrite(this Channel channel, Player player)
	{
		return player.ToBanchoPrivileges().HasPrivilege(channel.WritePrivileges);
	}

	public static void SendMessage(this Channel channel, Message message, bool toSelf = false)
	{
		using var messagePacket = new ServerPackets();
		messagePacket.SendMessage(new Message
		{
			Sender = message.Sender,
			Content = message.Content,
			Destination = channel.Name,
			SenderId = message.SenderId
		});
		foreach (var player in channel.Players)
		{
			if (!player.BlockedByPlayer(message.SenderId) && (toSelf || player.Id != message.SenderId))
				player.Enqueue(messagePacket.GetContent());
		}
	}

	public static void SendBotMessage(this Channel channel, string message)
	{
		var bot = Session.BanchoBot;

		if (message.Length >= 31979)
			message = $"message would have crashed games ({message.Length} characters).";
		
		using var messagePacket = new ServerPackets();
		messagePacket.SendMessage(new Message
		{
			Sender = bot.Username,
			Content = message,
			Destination = channel.Name,
			SenderId = bot.Id
		});
		channel.EnqueueToPlayers(messagePacket.GetContent());
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