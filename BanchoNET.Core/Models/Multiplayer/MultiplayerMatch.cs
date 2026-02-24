using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Multiplayer;

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
	public ushort Id { get; init; }
	
	/// <summary>
	/// Used by database for identification
	/// </summary>
	public int LobbyId { get; init; }
	public int OnlineId => LobbyId;
	
	public required string Name { get; set; }
	public required string Password { get; set; }
	public GameMode Mode { get; set; }
	public int HostId { get; set; }
	public int CreatorId { get; set; }
	public List<int> Refs { get; } = [];
	public List<int> BannedPlayers { get; } = [];
	public List<int> TourneyClients { get; } = [];
	public Mods Mods { get; set; }
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
	public DateTime MapFinishDate { get; set; }
	public Channel Chat { get; set; } = null!;
	public MultiplayerSlot[] Slots { get; set; }
	public bool Locked { get; set; }
	public LobbyTimer Timer { get; set; }

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
		Timer.Dispose();
	}
}