namespace BanchoNET.Objects.Players;

public class PlayerStatus
{
	public Activity Activity { get; set; }
	public string ActivityDescription { get; set; }
	public string BeatmapMD5 { get; set; }
	public Mods CurrentMods { get; set; }
	public GameMode Mode { get; set; }
	public int BeatmapId { get; set; }
}