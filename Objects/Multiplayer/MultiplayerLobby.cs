using BanchoNET.Objects.Channels;

namespace BanchoNET.Objects.Multiplayer;

public class MultiplayerLobby
{
	public short MultiId { get; set; }
	public string Name { get; set; }
	public string? Password { get; set; }
	public GameMode Mode { get; set; }
	public int HostId { get; set; }
	public List<int> Refs { get; set; } = [];
	public Mods Mods { get; set; }
	public bool Freemods { get; set; }
	public int BeatmapId { get; set; }
	public int PreviousBeatmapId { get; set; }
	public string BeatmapName { get; set; }
	public string BeatmapMD5 { get; set; }
	public WinCondition WinCondition { get; set; }
	public LobbyType Type { get; set; }
	public bool InProgress { get; set; }
	public int Seed { get; set; }
	public Channel Chat { get; set; }
	public MultiplayerSlot[] Slots { get; set; } = new MultiplayerSlot[16];
	
	//TODO Starting timer
}