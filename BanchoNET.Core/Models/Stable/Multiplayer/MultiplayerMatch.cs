using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Stable.Multiplayer;

public sealed class MultiplayerMatch : IMultiplayerMatch, IDisposable,
	IEquatable<MultiplayerMatch>
{
	public MultiplayerMatch()
	{
		Slots = new MultiplayerSlot[16];

		for (int i = 0; i < Slots.Length; i++)
			Slots[i] = new MultiplayerSlot();
	}
	
	/// <summary>
	/// Used by osu for multiplayer lobby identification
	/// </summary>
	public ushort Id { get; set; }
	
	/// <summary>
	/// Used by database for identification
	/// </summary>
	public long LobbyId { get; set; }
	public long OnlineId => LobbyId;
	
	public required string Name { get; set; }
	public required string Password { get; set; }
	public GameMode Mode { get; set; }
	public int HostId { get; set; }
	public int CreatorId { get; set; }
	public List<int> Refs { get; } = [];
	public List<int> BannedPlayers { get; } = [];
	public List<int> TourneyClients { get; } = [];
	public LegacyMods Mods { get; set; }
	public bool Freemods { get; set; }
	public int BeatmapId { get; set; }
	public int PreviousBeatmapId { get; set; }
	public required string BeatmapName { get; set; }
	public required string BeatmapMD5 { get; set; }
	public WinCondition WinCondition { get; set; }
	public LobbyType Type { get; set; }
	public bool InProgress { get; set; }
	public byte Powerplay { get; set; }
	public int Seed { get; set; }
	public Channel Chat { get; set; } = null!;
	public MultiplayerSlot[] Slots { get; set; }
	public bool Locked { get; set; }
	public LobbyTimer? Timer { get; set; }

	/// <summary>
	/// Caps how long a client that never reports back can block the lobby.
	/// </summary>
	public MatchTimeout CompleteTimeout { get; } = new();
	public MatchTimeout LoadTimeout { get; } = new();

	#region Game tracking

	private const int TrackedGames = 3;

	private readonly Lock _gamesLock = new();
	private readonly List<TrackedGame> _recentGames = [];

	private sealed class TrackedGame(long gameId, string beatmapMd5)
	{
		public long GameId { get; } = gameId;
		public string BeatmapMD5 { get; } = beatmapMd5;
		public HashSet<int> SubmittedBy { get; } = [];
	}

	/// <summary>
	/// The game scores are currently expected for, or null between maps.
	/// </summary>
	public long? CurrentGameId
	{
		get
		{
			lock (_gamesLock) return _recentGames.Count == 0 ? null : _recentGames[^1].GameId;
		}
	}

	public void TrackGame(
		long gameId,
		string beatmapMd5
	) {
		lock (_gamesLock)
		{
			_recentGames.Add(new TrackedGame(gameId, beatmapMd5));

			if (_recentGames.Count > TrackedGames)
				_recentGames.RemoveRange(0, _recentGames.Count - TrackedGames);
		}
	}

	/// <summary>
	/// Claims a place on a scoreboard for a score that just came in, matching on the beatmap rather
	/// than on when it arrived. Normally that is the current game; a straggler submitting after the
	/// next map started still resolves to the game they actually played, and the same map played
	/// twice in a row is disambiguated by skipping games the player already has a score on.
	/// <para>
	/// Claiming here rather than on write means a player cannot occupy two slots with one score.
	/// </para>
	/// </summary>
	/// <returns>The game to attribute the score to, or null if none of the tracked games fit.</returns>
	public long? ClaimGameFor(
		string beatmapMd5,
		int playerId
	) {
		lock (_gamesLock)
		{
			for (var i = _recentGames.Count - 1; i >= 0; i--)
			{
				var game = _recentGames[i];

				if (game.BeatmapMD5 != beatmapMd5 || !game.SubmittedBy.Add(playerId)) continue;

				return game.GameId;
			}

			return null;
		}
	}

	#endregion

	#region IEquatable

	public bool Equals(MultiplayerMatch? other) => this.MatchesOnlineId(other);
	
	public override bool Equals(
		object? obj
	) {
		return ReferenceEquals(this, obj) || obj is MultiplayerMatch other && Equals(other);
	}
	
	public override int GetHashCode() => HashCode.Combine(Id, LobbyId);

	#endregion
	
	public void Dispose() {
		Timer?.Dispose();
		CompleteTimeout.Dispose();
		LoadTimeout.Dispose();
	}
}