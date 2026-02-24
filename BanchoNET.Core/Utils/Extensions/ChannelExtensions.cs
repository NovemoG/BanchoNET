using System.Collections.Immutable;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Packets;

namespace BanchoNET.Core.Utils.Extensions;

public static class ChannelExtensions
{
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
	
	extension(
		Channel channel
	) {
		public bool PlayerInChannel(
			Player player
		) {
			return channel.Players.Any(p => p.Id == player.Id);
		}

		public bool CanPlayerRead(
			Player player
		) {
			return player.ToBanchoPrivileges().CompareHighestPrivileges(channel.ReadPrivileges);
		}

		public bool CanPlayerWrite(
			Player player
		) {
			return player.ToBanchoPrivileges().CompareHighestPrivileges(channel.WritePrivileges);
		}

		public void SendMessage(
			Message message,
			bool toSelf = false
		) {
			var messageBytes = new ServerPackets()
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

		public void SendBotMessage(
			string message,
			Player from
		) {
			if (message.Length >= 31979)
				message = $"message would have crashed games ({message.Length} characters).";
		
			channel.EnqueueToPlayers(new ServerPackets()
				.SendMessage(new Message
				{
					Sender = from.Username,
					Content = message,
					Destination = channel.Name,
					SenderId = from.Id
				})
				.FinalizeAndGetContent());
		}

		public void EnqueueToPlayers(
			byte[] data,
			List<int>? immune = null
		) {
			immune ??= [];
		
			foreach (var player in channel.Players)
				if (!immune.Remove(player.Id))
					player.Enqueue(data);
		}
	}
}