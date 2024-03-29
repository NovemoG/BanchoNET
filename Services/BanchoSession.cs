﻿using System.Collections.Concurrent;
using System.Net;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Channel = BanchoNET.Objects.Channels.Channel;

namespace BanchoNET.Services;

public sealed class BanchoSession
{
	private static readonly Lazy<BanchoSession> Lazy = new(() => new BanchoSession());
	public static BanchoSession Instance => Lazy.Value;
	
	private BanchoSession()
	{
		BanchoBot = Bots[1];
	}
	
	#region PlayersCollections
	
	public readonly ConcurrentDictionary<int, Player> Players = [];
	public readonly ConcurrentDictionary<int, Player> Restricted = [];
	public readonly ConcurrentDictionary<int, Player> Bots = new()
	{
		[1] = new Player(new PlayerDto { Id = 1 })
		{
			Username = "Nyzrum Bot", //TODO laod from config/db
			LastActivityTime = DateTime.MaxValue,
			Privileges = Privileges.Verified | Privileges.Staff,
		}
	};

	public readonly Player BanchoBot;

	#endregion
	
	private readonly ConcurrentDictionary<string, string> _passwordHashes = [];
	private readonly ConcurrentDictionary<string, IPAddress> _ipCache = [];
	
	#region Channels

	private readonly List<Channel> _spectatorChannels = [];
	private readonly List<Channel> _channels =
	[
		new Channel
		{
			Id = 0,
			Name = "#osu",
			Description = "Main osu! chatroom",
			AutoJoin = true,
			Hidden = false,
			ReadOnly = false,
			Instance = false,
			ReadPrivileges = ClientPrivileges.Player,
			WritePrivileges = ClientPrivileges.Player,
			Players = []
		},
		new Channel
		{
			Id = 1,
			Name = "#lobby",
			Description = "Multiplayer chatroom",
			AutoJoin = false,
			Hidden = false,
			ReadOnly = false,
			Instance = false,
			ReadPrivileges = ClientPrivileges.Player,
			WritePrivileges = ClientPrivileges.Player,
			Players = []
		},
		new Channel
		{
			Id = 2,
			Name = "#staff",
			Description = "osu! staff chatroom",
			AutoJoin = false,
			Hidden = true,
			ReadOnly = false,
			Instance = false,
			ReadPrivileges = ClientPrivileges.Owner,
			WritePrivileges = ClientPrivileges.Owner,
			Players = []
		},
	];

	#endregion

	public void InsertPasswordHash(string passwordMD5, string passwordHash)
	{
		_passwordHashes.TryAdd(passwordHash, passwordMD5);
	}
	
	public bool CheckHashes(string passwordMD5, string passwordHash)
	{
		if (_passwordHashes.TryGetValue(passwordHash, out var md5))
			return md5 == passwordMD5;

		if (!BCrypt.Net.BCrypt.Verify(passwordMD5, passwordHash)) 
			return false;
		
		_passwordHashes.TryAdd(passwordHash, passwordMD5);
		return true;
	}

	public IPAddress? GetCachedIp(string ipString)
	{
		_ipCache.TryGetValue(ipString, out var ip);
		return ip;
	}

	public IPAddress CacheIp(string ipString)
	{
		var ip = IPAddress.Parse(ipString);
		_ipCache.TryAdd(ipString, ip);
		return ip;
	}
	
	public void AppendPlayer(Player player)
	{
		if (player.Restricted) Restricted.TryAdd(player.Id, player);
		else Players.TryAdd(player.Id, player);
	}

	public bool LogoutPlayer(Player player)
	{
		Console.WriteLine($"[{GetType().Name}] Logout time difference: {DateTime.UtcNow - player.LoginTime}");
		if (DateTime.UtcNow - player.LoginTime < TimeSpan.FromSeconds(1)) return false;
		
		if (player.Lobby != null) player.LeaveMatch();

		player.Spectating?.RemoveSpectator();

		while (player.Channels.Count != 0)
			player.LeaveChannel(player.Channels[0], false);

		if (!player.Restricted)
		{
			using var logoutPacket = new ServerPackets();
			logoutPacket.Logout(player.Id);
			EnqueueToPlayers(logoutPacket.GetContent());
		}
		
		if (!Players.TryRemove(player.Id, out _))
			Console.WriteLine($"[{GetType().Name}] Failed to remove {player.Id} from session");

		Console.Write($"[{GetType().Name}] Players left: {Players.Count}, names: ");
		foreach (var user in Players)
		{
			Console.Write($"{user.Value.Username}, ");
		}
		Console.Write('\n');

		return true;
	}
	
	public Player? GetPlayer(int id = 1, string username = "", Guid token = new())
	{
		Player? sessionPlayer;
		
		if (id > 1)
		{
			Bots.TryGetValue(id, out sessionPlayer);
			if (sessionPlayer != null) return sessionPlayer;
			Players.TryGetValue(id, out sessionPlayer);
			if (sessionPlayer != null) return sessionPlayer;
			Restricted.TryGetValue(id, out sessionPlayer);
			return sessionPlayer;
		}

		if (username != string.Empty)
		{
			sessionPlayer = Bots.FirstOrDefault(p => p.Value.Username == username).Value;
			if (sessionPlayer != null) return sessionPlayer;
			sessionPlayer = Players.FirstOrDefault(r => r.Value.Username == username).Value;
			return sessionPlayer ?? Restricted.FirstOrDefault(b => b.Value.Username == username).Value;
		}

		if (token != Guid.Empty)
		{
			sessionPlayer = Players.FirstOrDefault(p => p.Value.Token == token).Value;
			return sessionPlayer ?? Restricted.FirstOrDefault(r => r.Value.Token == token).Value;
		}

		return null;
	}
	
	public Channel? GetChannel(string name, bool spectator = false)
	{
		return spectator ? _spectatorChannels.FirstOrDefault(c => c.Name == name) 
			: _channels.FirstOrDefault(c => c.Name == name);
	}
	
	public List<Channel> GetAutoJoinChannels(Player player)
	{
		var joinChannels = new List<Channel>();

		foreach (var channel in _channels)
		{
			if (!channel.AutoJoin || 
			    !channel.CanPlayerRead(player) ||
			    channel.Name == "#lobby")
			{
				continue;
			}

			joinChannels.Add(channel);
			
			//TODO Send to all players present in the channel to update their player count
		}

		return joinChannels;
	}

	public void EnqueueToPlayers(byte[] data)
	{
		foreach (var player in Players.Values)
			player.Enqueue(data);

		foreach (var player in Restricted.Values)
			player.Enqueue(data);
	}
}