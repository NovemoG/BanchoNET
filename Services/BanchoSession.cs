using System.Collections.Concurrent;
using BanchoNET.Abstractions.Services;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils.Extensions;
using Channel = BanchoNET.Objects.Channels.Channel;

namespace BanchoNET.Services;

public sealed class BanchoSession : IBanchoSession
{
	#region Instance

	private static readonly Lazy<BanchoSession> Lazy = new(() => new BanchoSession());
	public static BanchoSession Instance => Lazy.Value;
	
	private BanchoSession() { }

	#endregion
	
	#region Players
	
	public Player BanchoBot => _botsById[1];
	
	private readonly ConcurrentDictionary<Guid, Player> _playersByToken = [];
	private readonly ConcurrentDictionary<string, Player> _playersByUsername = [];
	private readonly ConcurrentDictionary<int, Player> _playersById = [];
	public IEnumerable<Player> Players => _playersById.Values;
	public IEnumerable<Player> PlayersInLobby => Players.Where(p => p.InLobby);

	private readonly ConcurrentDictionary<Guid, Player> _restrictedByToken = [];
	private readonly ConcurrentDictionary<string, Player> _restrictedByUsername = [];
	private readonly ConcurrentDictionary<int, Player> _restrictedById = [];
	public IEnumerable<Player> Restricted => _restrictedById.Values;
	
	private readonly ConcurrentDictionary<string, Player> _botsByUsername = [];
	private readonly ConcurrentDictionary<int, Player> _botsById = [];
	public IEnumerable<Player> Bots => _botsById.Values;

	#endregion

	#region Beatmaps

	private readonly ConcurrentDictionary<int, BeatmapSet> _beatmapSetsCache = []; // setId -> BeatmapSet
	private readonly ConcurrentDictionary<string, Beatmap> _beatmapMD5Cache = []; // MD5 -> Beatmap
	private readonly ConcurrentDictionary<int, Beatmap> _beatmapIdCache = []; // mapId -> Beatmap
	private readonly List<string> _notSubmittedBeatmaps = []; // MD5s
	private readonly List<string> _needUpdateBeatmaps = []; // MD5s

	#endregion
	
	#region Channels

	private readonly ConcurrentDictionary<string, Channel> _spectatorChannels = [];
	private readonly ConcurrentDictionary<string, Channel> _channels = [];
	public IEnumerable<Channel> Channels => _channels.Values;

	public Channel LobbyChannel
	{
		get
		{
			if (_channels.TryGetValue("#lobby", out var lobby))
				return lobby;
			
			Logger.Shared.LogWarning("Couldn't find '#lobby' channel, creating a default one.", caller: nameof(BanchoSession));

			lobby = ChannelExtensions.DefaultChannels.First(c => c.IdName == "#lobby");
			_channels.TryAdd("#lobby", lobby);
			return lobby;
		}
	}

	#endregion

	#region Other

	private readonly ConcurrentDictionary<int, MultiplayerLobby> _multiplayerLobbies = [];
	public IEnumerable<MultiplayerLobby> Lobbies => _multiplayerLobbies.Values;
	
	private readonly ConcurrentDictionary<string, string> _passwordHashes = [];

	#endregion

	public void ClearPasswordsCache() => _passwordHashes.Clear();
	
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
	
	public void AppendBot(Player bot)
	{
		bot.IsBot = true;
		
		_botsById.TryAdd(bot.Id, bot);
		_botsByUsername.TryAdd(bot.Username.MakeSafe(), bot);
	}
	
	public void AppendPlayer(Player player)
	{
		if (player.Restricted)
		{
			_restrictedByToken.TryAdd(player.Token, player);
			_restrictedByUsername.TryAdd(player.Username.MakeSafe(), player);
			_restrictedById.TryAdd(player.Id, player);
		}
		else
		{
			_playersByToken.TryAdd(player.Token, player);
			_playersByUsername.TryAdd(player.Username.MakeSafe(), player);
			_playersById.TryAdd(player.Id, player);
		}
	}

	public bool LogoutPlayer(Player player)
	{
		Logger.Shared.LogDebug($"Logout time difference: {DateTime.Now - player.LoginTime}", nameof(BanchoSession));
		if (DateTime.Now - player.LoginTime < TimeSpan.FromSeconds(1)) return false;
		
		if (player.Lobby != null) player.LeaveMatch();

		player.Spectating?.RemoveSpectator(player);

		while (player.Channels.Count != 0)
			player.LeaveChannel(player.Channels[0], false);

		if (!player.Restricted)
		{
			EnqueueToPlayers(new ServerPacket()
				.Logout(player.Id)
				.FinalizeAndGetContent());
		}

		var successfullyRemoved = _playersById.TryRemove(player.Id, out _)
		                          && _playersByUsername.TryRemove(player.Username.MakeSafe(), out _)
		                          && _playersByToken.TryRemove(player.Token, out _);
		if (!successfullyRemoved)
		{
			Logger.Shared.LogWarning($"[{GetType().Name}] Failed to remove {player.Username} from session", caller: nameof(BanchoSession));
		}

		return true;
	}

	public Player? GetPlayerById(int id)
	{
		if (id < 1) return null;

		_playersById.TryGetValue(id, out var sessionPlayer);
		if (sessionPlayer != null) return sessionPlayer;
		
		_botsById.TryGetValue(id, out sessionPlayer);
		if (sessionPlayer != null) return sessionPlayer;
		
		_restrictedById.TryGetValue(id, out sessionPlayer);
		return sessionPlayer;
	}

	public Player? GetPlayerByName(string? username)
	{
		if (string.IsNullOrEmpty(username)) return null;

		username = username.MakeSafe();
		
		_playersByUsername.TryGetValue(username, out var sessionPlayer);
		if (sessionPlayer != null) return sessionPlayer;
		
		_botsByUsername.TryGetValue(username, out sessionPlayer);
		if (sessionPlayer != null) return sessionPlayer;
		
		_restrictedByUsername.TryGetValue(username, out sessionPlayer);
		return sessionPlayer;
	}

	public Player? GetPlayerByToken(Guid token)
	{
		return _playersByToken.TryGetValue(token, out var sessionPlayer)
			? sessionPlayer
			: _restrictedByToken.TryGetValue(token, out sessionPlayer)
				? sessionPlayer
				: null;
	}
	
	public Channel? GetChannel(string name, bool spectator = false)
	{
		if (spectator)
		{
			if (_spectatorChannels.TryGetValue(name, out var channel))
				return channel;
		}
		else
		{
			if (_channels.TryGetValue(name, out var channel))
				return channel;
		}

		return null;
	}
	
	public void InsertChannel(Channel channel, bool spectator = false)
	{
		if (spectator) _spectatorChannels.TryAdd(channel.IdName, channel);
		else _channels.TryAdd(channel.IdName, channel);
	}
	
	public Beatmap? GetBeatmapByMD5(string beatmapMD5)
	{
		return _beatmapMD5Cache.TryGetValue(beatmapMD5, out var cachedBeatmap) ? cachedBeatmap : null;
	}
	
	public Beatmap? GetBeatmapById(int mapId)
	{
		return _beatmapIdCache.TryGetValue(mapId, out var cachedBeatmap) ? cachedBeatmap : null;
	}
	
	public BeatmapSet? GetBeatmapSet(int setId)
	{
		return _beatmapSetsCache.TryGetValue(setId, out var beatmapSet) ? beatmapSet : null;
	}

	public void CacheBeatmapSet(BeatmapSet set)
	{
		Logger.Shared.LogDebug($"Caching beatmap set with id: {set.Id}", nameof(BanchoSession));
		
		_beatmapSetsCache.AddOrUpdate(set.Id, set, (_, _) => set);

		foreach (var beatmap in set.Beatmaps)
		{
			_beatmapMD5Cache.AddOrUpdate(beatmap.MD5, beatmap, (_, _) => beatmap);
			_beatmapIdCache.AddOrUpdate(beatmap.MapId, beatmap, (_, _) => beatmap);
		}
	}

	public bool IsBeatmapNotSubmitted(string beatmapMD5)
	{
		return _notSubmittedBeatmaps.Contains(beatmapMD5);
	}
	
	public void CacheNotSubmittedBeatmap(string beatmapMD5)
	{
		Logger.Shared.LogDebug($"Caching not submitted beatmap with MD5: {beatmapMD5}", nameof(BanchoSession));
		
		_notSubmittedBeatmaps.Add(beatmapMD5);
	}
	
	public bool BeatmapNeedsUpdate(string beatmapMD5)
	{
		return _needUpdateBeatmaps.Contains(beatmapMD5);
	}
	
	public void CacheNeedsUpdateBeatmap(string beatmapMD5)
	{
		Logger.Shared.LogDebug($"Caching beatmap which needs update with MD5: {beatmapMD5}", nameof(BanchoSession));
		
		_needUpdateBeatmaps.Add(beatmapMD5);
	}

	public ushort GetFreeMatchId()
	{
		for (ushort i = 0; i < _multiplayerLobbies.Count; i++)
		{
			if (_multiplayerLobbies[i].Id != i)
				return i;
		}

		return (ushort)_multiplayerLobbies.Count;
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
		
		Logger.Shared.LogDebug($"Removing lobby with id: {lobby.Id}", nameof(BanchoSession));
	}

	public void EnqueueToPlayers(byte[] data)
	{
		foreach (var player in _playersById.Values)
			player.Enqueue(data);

		foreach (var player in _restrictedById.Values)
			player.Enqueue(data);
	}
}