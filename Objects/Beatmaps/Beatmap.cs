namespace BanchoNET.Objects.Beatmaps;

public class Beatmap
{
	public int Id { get; set; }
	public int BeatmapId { get; set; }
	public int BeatmapsetId { get; set; }
	public string BeatmapMD5 { get; set; }

	public byte Mode { get; set; }
	public string Artist { get; set; }
	public string Title { get; set; }
	public string DiffName { get; set; }
	public string Creator { get; set; }

	public float Cs { get; set; }
	public float Ar { get; set; }
	public float Od { get; set; }
	public float Hp { get; set; }
	public float Drain { get; set; }
	public int MaxCombo { get; set; }
	public int HitLength { get; set; }
	public int Bpm { get; set; }
	public int RankedStatus { get; set; }
	public bool StatusFrozen { get; set; }
	public DateTime LastUpdate { get; set; }
	public long Plays { get; set; }
	public long Passes { get; set; }

	public string FileName { get; set; }
}