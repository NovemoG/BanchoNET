namespace BanchoNET.Objects.Player;

public class PlayerStats
{
	public int PlayerId { get; set; }
	public byte Activity { get; set; }
	public string ActivityDescription { get; set; }
	public string BeatmapMD5 { get; set; }
	public int CurrentMods { get; set; }
	public byte Mode { get; set; }
	public int BeatmapId { get; set; }
	public long RankedScore { get; set; }
	public float Accuracy { get; set; }
	public int PlayCount { get; set; }
	public long TotalScore { get; set; }
	public int Rank { get; set; }
	public short PP { get; set; }
}