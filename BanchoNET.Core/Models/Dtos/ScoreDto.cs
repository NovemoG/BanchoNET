using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanchoNET.Core.Models.Dtos;

public class ScoreDto
{
	public long Id { get; set; }
	
	public required string BeatmapMD5 { get; set; }
	public int MapId { get; set; }
	
	public bool IsPinned { get; set; }
	public bool Preserve { get; set; }
	public bool Processed { get; set; }
	public bool Ranked { get; set; }
	public bool HasReplay { get; set; }
	[NotMapped] public bool Passed => Status > 0;
	
	public float PP { get; set; }
	public float Acc { get; set; }
	public int MaxCombo { get; set; }
	public int Mods { get; set; }
	[MaxLength(512)]
	public string? LazerMods { get; set; }
	public int Count300 { get; set; }
	public int Count100 { get; set; }
	public int Count50 { get; set; }
	public int Misses { get; set; }
	/// <summary>
	/// On lazer used as LargeTickHit
	/// </summary>
	public int Gekis { get; set; }
	/// <summary>
	/// On lazer used as SliderTailHit
	/// </summary>
	public int Katus { get; set; }
	public int IgnoreHit { get; set; }
	public int IgnoreMiss { get; set; }
	
	public int TotalScore { get; set; }
	public int ClassicScore { get; set; }
	public int TotalScoreWithoutMods { get; set; }
	public int LegacyTotalScore { get; set; }
	
	public byte Grade { get; set; }
	public byte Status { get; set; }
	public byte Mode { get; set; }
	
	/// <summary>
	/// On lazer used as EndedAt
	/// </summary>
	public DateTimeOffset PlayTime { get; set; }
	public DateTimeOffset? StartTime { get; set; }
	
	public int TimeElapsed { get; set; }
	public int ClientFlags { get; set; }
	public bool LegacyPerfect { get; set; }
	public bool IsPerfectCombo { get; set; }
	
	public string? OnlineChecksum { get; set; }

	//TODO
	public bool IsRestricted { get; set; }
	
	[ForeignKey("PlayerId")]
	public PlayerDto Player { get; set; } = null!;
	public int PlayerId { get; set; }
	
	[ForeignKey("BeatmapId")]
	public BeatmapDto Beatmap { get; set; } = null!;
}