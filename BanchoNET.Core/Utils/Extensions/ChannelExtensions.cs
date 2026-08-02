using System.Collections.Immutable;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;

namespace BanchoNET.Core.Utils.Extensions;

public static class ChannelExtensions
{
	public static readonly ImmutableList<Channel> DefaultChannels = ImmutableList.Create(
		new Channel("#osu", 5)
		{
			Description = "Main osu! chatroom",
			AutoJoin = true,
			Hidden = false,
			ReadOnly = false,
			Instance = false,
			Type = ChannelType.Public,
			ReadPrivileges = ClientPrivileges.Player,
			WritePrivileges = ClientPrivileges.Player,
		},
		new Channel("#lobby", 6)
		{
			Description = "Multiplayer chatroom",
			AutoJoin = false,
			Hidden = false,
			ReadOnly = false,
			Instance = false,
			Type = ChannelType.Public,
			ReadPrivileges = ClientPrivileges.Player,
			WritePrivileges = ClientPrivileges.Player,
		},
		new Channel("#announce", 7)
		{
			Description = "Chatroom for announcements about scores and maps",
			AutoJoin = false,
			Hidden = false,
			ReadOnly = true,
			Instance = false,
			Type = ChannelType.Announce,
			ReadPrivileges = ClientPrivileges.Player,
			WritePrivileges = ClientPrivileges.Player,
		},
		new Channel("#staff", 8)
		{
			Description = "osu! staff chatroom",
			AutoJoin = false,
			Hidden = true,
			ReadOnly = false,
			Instance = false,
			Type = ChannelType.Private,
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