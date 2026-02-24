namespace BanchoNET.Core.Models.Players;

public class PlayerStatus
{
	public Activity Activity { get; set; } = Activity.Idle;
	public string ActivityDescription { get; set; } = string.Empty;
	public string BeatmapMD5 { get; set; } = string.Empty;
	public Mods CurrentMods { get; set; } = Mods.None;
	public GameMode Mode { get; set; } = GameMode.VanillaStd;
	public int BeatmapId { get; set; }
}