using BanchoNET.Models;

namespace BanchoNET.Objects.Beatmaps;

public class Beatmap
{
	public int MapId { get; set; }
	public int SetId { get; set; }
	public bool Private { get; set; }
	public GameMode Mode { get; set; }
	public BeatmapStatus Status { get; set; }
	
	public string MD5 { get; set; }
	
	public string Artist { get; set; }
	public string Title { get; set; }
	public string Name { get; set; }
	public string Creator { get; set; }
	public string FileName { get; set; }

	public DateTime LastUpdate { get; set; }
	public int TotalLength { get; set; }
	public int MaxCombo { get; set; }
	public bool StatusFrozen { get; set; }
	public long Plays { get; set; }
	public long Passes { get; set; }
	
	public float Bpm { get; set; }
	public float Cs { get; set; }
	public float Ar { get; set; }
	public float Od { get; set; }
	public float Hp { get; set; }
	public float StarRating { get; set; }

	public Beatmap(ApiBeatmap apiBeatmap)
	{
		
	}
}