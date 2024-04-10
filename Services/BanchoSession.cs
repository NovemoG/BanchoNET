using System.Collections.Concurrent;
using System.Net;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Multiplayer;
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
	
	#region PlayersCollections
	
	public Player BanchoBot => _bots[1];
	
	private readonly ConcurrentDictionary<int, Player> _players = [];
	public IEnumerable<Player> Players => _players.Values;
	
	private readonly ConcurrentDictionary<int, Player> _restricted = [];
	public IEnumerable<Player> Restricted => _restricted.Values;
	
	private readonly ConcurrentDictionary<int, Player> _bots = [];
	public IEnumerable<Player> Bots => _bots.Values;

	#endregion

	#region Beatmaps

	private readonly ConcurrentDictionary<int, BeatmapSet> _beatmapSetsCache = [];
	private readonly ConcurrentDictionary<string, Beatmap> _beatmapMD5Cache = [];
	private readonly ConcurrentDictionary<int, Beatmap> _beatmapIdCache = [];
	private readonly List<string> _notSubmittedBeatmaps = [];
	private readonly List<string> _needUpdateBeatmaps = [];

	#endregion
	
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
			Name = "#announce",
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
			Id = 3,
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

	#region Other

	private readonly ConcurrentDictionary<int, MultiplayerLobby> _multiplayerLobbies = [];
	public IEnumerable<MultiplayerLobby> Lobbies => _multiplayerLobbies.Values;
	
	private readonly ConcurrentDictionary<string, string> _passwordHashes = [];
	private readonly ConcurrentDictionary<string, IPAddress> _ipCache = [];

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
	
	public void AppendBot(Player bot)
	{
		_bots.TryAdd(bot.Id, bot);
	}
	
	public void AppendPlayer(Player player)
	{
		if (player.Restricted) _restricted.TryAdd(player.Id, player);
		else _players.TryAdd(player.Id, player);
	}

	public bool LogoutPlayer(Player player)
	{
		Console.WriteLine($"[{GetType().Name}] Logout time difference: {DateTime.Now - player.LoginTime}");
		if (DateTime.Now - player.LoginTime < TimeSpan.FromSeconds(1)) return false;
		
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
		
		if (!_players.TryRemove(player.Id, out _))
			Console.WriteLine($"[{GetType().Name}] Failed to remove {player.Id} from session");

		Console.Write($"[{GetType().Name}] Players left: {_players.Count}, names: ");
		foreach (var user in _players)
		{
			Console.Write($"{user.Value.Username}, ");
		}
		Console.Write('\n');

		return true;
	}
	
	public Player? GetPlayer(int id = 0, string username = "", Guid token = new())
	{
		Player? sessionPlayer;
		
		if (id > 0)
		{
			_players.TryGetValue(id, out sessionPlayer);
			if (sessionPlayer != null) return sessionPlayer;
			_restricted.TryGetValue(id, out sessionPlayer);
			return sessionPlayer;
		}

		if (username != string.Empty)
		{
			sessionPlayer = _bots.Values.FirstOrDefault(p => p.Username == username);
			if (sessionPlayer != null) return sessionPlayer;
			sessionPlayer = _players.Values.FirstOrDefault(r => r.Username == username);
			return sessionPlayer ?? _restricted.Values.FirstOrDefault(b => b.Username == username);
		}

		if (token != Guid.Empty)
		{
			sessionPlayer = _players.Values.FirstOrDefault(p => p.Token == token);
			return sessionPlayer ?? _restricted.Values.FirstOrDefault(r => r.Token == token);
		}

		return null;
	}

	public Player? GetBot(int id = 0, string username = "")
	{
		if (id <= 0)
			return username != string.Empty
				? _bots.Values.FirstOrDefault(p => p.Username == username)
				: null;
		
		_bots.TryGetValue(id, out var sessionPlayer);
		return sessionPlayer;
	}
	
	public Channel? GetChannel(string name, bool spectator = false)
	{
		return spectator ? _spectatorChannels.FirstOrDefault(c => c.Name == name) 
			: _channels.FirstOrDefault(c => c.Name == name);
	}
	
	public void InsertChannel(Channel channel, bool spectator = false)
	{
		if (spectator) _spectatorChannels.Add(channel);
		else _channels.Add(channel);
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

	public Beatmap? GetBeatmap(string beatmapMD5 = "", int mapId = -1)
	{
		if (!string.IsNullOrEmpty(beatmapMD5))
		{
			if (_beatmapMD5Cache.TryGetValue(beatmapMD5, out var cachedBeatmap))
				return cachedBeatmap;
		}
		
		if (mapId > -1)
		{
			if (_beatmapIdCache.TryGetValue(mapId, out var cachedBeatmap))
				return cachedBeatmap;
		}

		return null;
	}
	
	public BeatmapSet? GetBeatmapSet(int setId)
	{
		return _beatmapSetsCache.TryGetValue(setId, out var beatmapSet) ? beatmapSet : null;
	}

	public void CacheBeatmapSet(BeatmapSet set)
	{
		Console.WriteLine($"[BanchoSession] Caching beatmap set with id: {set.Id}");
		
		_beatmapSetsCache.TryGetValue(set.Id, out var currentSet);

		if (currentSet != null)
			foreach (var beatmap in currentSet.Beatmaps)
            	_beatmapMD5Cache.TryRemove(beatmap.MD5, out _);
		
		_beatmapSetsCache.AddOrUpdate(set.Id, set, (_, _) => set);

		foreach (var beatmap in set.Beatmaps)
		{
			_beatmapMD5Cache.TryAdd(beatmap.MD5, beatmap);
			_beatmapIdCache.AddOrUpdate(beatmap.MapId, beatmap, (_, _) => beatmap);
		}
	}

	public bool IsBeatmapNotSubmitted(string beatmapMD5)
	{
		return _notSubmittedBeatmaps.Contains(beatmapMD5);
	}
	
	public void CacheNotSubmittedBeatmap(string beatmapMD5)
	{
		Console.WriteLine("[BanchoSession] Checking not submitted beatmaps for matching MD5");
		
		_notSubmittedBeatmaps.Add(beatmapMD5);
	}
	
	public bool BeatmapNeedsUpdate(string beatmapMD5)
	{
		return _needUpdateBeatmaps.Contains(beatmapMD5);
	}
	
	public void CacheNeedUpdateBeatmap(string beatmapMD5)
	{
		Console.WriteLine("[BanchoSession] Checking beatmaps which need update for matching MD5");
		
		_needUpdateBeatmaps.Add(beatmapMD5);
	}

	public MultiplayerLobby? GetLobby(ushort id)
	{
		_multiplayerLobbies.TryGetValue(id, out var lobby);
		return lobby;
	}
	
	public void InsertLobby(MultiplayerLobby lobby)
	{
		_multiplayerLobbies.TryAdd(lobby.Id, lobby);
	}
	
	public void RemoveLobby(MultiplayerLobby lobby)
	{
		_multiplayerLobbies.TryRemove(lobby.Id, out _);
	}

	public void EnqueueToPlayers(byte[] data)
	{
		foreach (var player in _players.Values)
			player.Enqueue(data);

		foreach (var player in _restricted.Values)
			player.Enqueue(data);
	}
}