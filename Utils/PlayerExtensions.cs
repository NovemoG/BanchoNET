using BanchoNET.Models;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Services;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Utils;

public static class PlayerExtensions
{
	private static readonly DbContextOptions<BanchoDbContext> DbOptions = new DbContextOptionsBuilder<BanchoDbContext>().UseMySQL("server=127.0.0.1;database=utopia;user=root;password=;").Options;
	
	private static readonly Dictionary<Privileges, ClientPrivileges> ClientPrivilegesMap = new()
	{
		{ Privileges.Unrestricted, ClientPrivileges.Player },
		{ Privileges.Supporter, ClientPrivileges.Supporter },
		{ Privileges.Moderator, ClientPrivileges.Moderator },
		{ Privileges.Administrator, ClientPrivileges.Developer },
		{ Privileges.Developer, ClientPrivileges.Owner },
	};

	public static string MakeSafe(this string name)
	{
		return name.Replace(" ", "_").ToLower();
	}
	
	public static ClientPrivileges ToBanchoPrivileges(this Player player)
	{
		var retPriv = 0;
		var privs = (int)player.Privileges;
		
		foreach (var priv in EnumExtensions.GetValues<Privileges>())
		{
			if ((privs & (int)priv) == (int)priv && ClientPrivilegesMap.TryGetValue(priv, out var value)) 
				retPriv |= (int)value;
		}

		return (ClientPrivileges)retPriv;
	}

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

		var channelInfoPacket = new ServerPackets();
		channelInfoPacket.ChannelInfo(channel);

		if (channel.Instance)
			foreach (var user in channel.Players)
				user.Enqueue(channelInfoPacket.GetContent());
		else
			foreach (var user in BanchoSession.Instance.Players.Where(channel.CanPlayerRead))
				user.Enqueue(channelInfoPacket.GetContent());

		return true;
	}
	
	public static void LeaveChannel(this Player player, Channel channel)
	{
		
	}
	
	public static void UpdateLatestActivity(this Player player)
	{
		using var dbContext = new BanchoDbContext(DbOptions);
		dbContext.Players.Where(p => p.Id == player.Id).ExecuteUpdate(p => p.SetProperty(u => u.LastActivityTime, DateTime.UtcNow));
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

	public static void Enqueue(this Player player, byte[] dataBytes)
	{
		player.Queue.WriteBytes(dataBytes);
	}
}