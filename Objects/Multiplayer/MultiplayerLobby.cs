using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Objects.Multiplayer;

public class MultiplayerLobby
{
	public MultiplayerLobby()
	{
		Slots = new MultiplayerSlot[16];

		for (int i = 0; i < Slots.Length; i++)
			Slots[i] = new MultiplayerSlot();
	}
	
	/// <summary>
	/// Used by database for identification
	/// </summary>
	public int LobbyId { get; set; }
	
	/// <summary>
	/// Used by osu for multiplayer lobby identification
	/// </summary>
	public ushort Id { get; set; }
	
	public required string Name { get; set; }
	public required string Password { get; set; }
	public GameMode Mode { get; set; }
	public int HostId { get; set; }
	public int CreatorId { get; set; }
	public List<int> Refs { get; set; } = [];
	public List<int> BannedPlayers { get; set; } = [];
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
	public Channel Chat { get; set; }
	public MultiplayerSlot[] Slots { get; set; }
	public bool IsLocked { get; set; }
	public LobbyTimer? Timer { get; set; }
}