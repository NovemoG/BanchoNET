namespace BanchoNET.Objects.Multiplayer;

public class MultiplayerLobby
{
	public short MultiId { get; set; }
	public bool InProgress { get; set; }
	public byte MultiType { get; set; }
	public string? Password { get; set; }
	public byte[] SlotStatus { get; set; }
	public int[] SlotId { get; set; }
	public byte[] SlotTeam { get; set; }
	public int[] SlotMods { get; set; }
	public bool[] LoadedPlayers { get; set; }
	public bool[] SkippedPlayers { get; set; }
	public bool[] FinishedPlayers { get; set; }
	public string BeatmapName { get; set; }
	public string BeatmapMD5 { get; set; }
	public int BeatmapId { get; set; }
	public int ActiveMods { get; set; }
	public int HostId { get; set; }
	public byte Mode { get; set; }
	public byte WinCondition { get; set; }
	public byte TeamType { get; set; }
	public byte SpecialMods { get; set; }
	public int Seed { get; set; }
}