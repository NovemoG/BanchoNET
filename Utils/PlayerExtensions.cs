using BanchoNET.Objects.Channels;
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

	/// <summary>
	/// Sends message directly to this player. If channel is provided, it will be sent to that channel instead.
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
	
	public static void LeaveMatch(this Player player)
	{
		
	}
	
	public static void RemoveSpectator(this Player player)
	{
		
	}

	public static bool BlockedByPlayer(this Player player, int targetId)
	{
		return player.Blocked.Contains(targetId);
	}

	public static bool JoinChannel(this Player player, Channel channel)
	{
		if (channel.PlayerInChannel(player) ||
		    !channel.CanPlayerRead(player) ||
		    (channel.Name == "#lobby" && !player.InLobby))
		{
			return false;
		}

		player.AddToChannel(channel);

		using var channelJoinPacket = new ServerPackets();
		channelJoinPacket.ChannelJoin(channel.Name);
		player.Enqueue(channelJoinPacket.GetContent());

		using var channelInfoPacket = new ServerPackets();
		channelInfoPacket.ChannelInfo(channel);

		if (channel.Instance)
			foreach (var user in channel.Players)
				user.Enqueue(channelInfoPacket.GetContent());
		else
			foreach (var user in Session.Players.Where(p => channel.CanPlayerRead(p.Value)))
				user.Value.Enqueue(channelInfoPacket.GetContent());
		
		return true;
	}
	
	public static void LeaveChannel(this Player player, Channel channel, bool kick = true)
	{
		if (!channel.PlayerInChannel(player)) return;
		
		player.RemoveFromChannel(channel);

		if (kick)
		{
			using var kickPacket = new ServerPackets();
			kickPacket.ChannelKick(channel.Name);
			player.Enqueue(kickPacket.GetContent());
		}
		
		using var channelInfoPacket = new ServerPackets();
		channelInfoPacket.ChannelInfo(channel);

		if (channel.Instance)
			foreach (var user in channel.Players)
				user.Enqueue(channelInfoPacket.GetContent());
		else
			foreach (var user in Session.Players.Where(p => channel.CanPlayerRead(p.Value)))
				user.Value.Enqueue(channelInfoPacket.GetContent());
		
		Console.WriteLine($"[PlayerExtensions] {player.Username} left {channel.Name}");
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