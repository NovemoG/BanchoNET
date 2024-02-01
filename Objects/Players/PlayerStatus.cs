namespace BanchoNET.Objects.Players;

public class PlayerStatus
{
	public byte Activity { get; set; }
	public string ActivityDescription { get; set; }
	public string BeatmapMD5 { get; set; }
	public int CurrentMods { get; set; }
	public byte Mode { get; set; }
	public int BeatmapId { get; set; }
}