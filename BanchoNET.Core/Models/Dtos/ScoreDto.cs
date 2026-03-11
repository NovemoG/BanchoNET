using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

[Index(nameof(PP))]
[Index(nameof(LegacyTotalScore))]
[Index(nameof(Mods))]
[Index(nameof(BeatmapMD5))]
[Index(nameof(OnlineChecksum))]
[Index(nameof(Mode))]
[Index(nameof(Status))]
[Index(nameof(PlayTime))]
[PrimaryKey(nameof(Id))]
public class ScoreDto
{
	[Key] public long Id { get; set; }
	
	[Column(TypeName = "CHAR(32)"), Unicode(false)]
	public required string BeatmapMD5 { get; set; }
	
	public bool IsPinned { get; set; }
	public bool Preserve { get; set; }
	public bool HasReplay { get; set; }
	[NotMapped] public bool Passed => Grade == 1;
	
	[Column(TypeName = "FLOAT(7,3)")]
	public float PP { get; set; }
	[Column(TypeName = "FLOAT(6,3)")]
	public float Acc { get; set; }
	public int MaxCombo { get; set; }
	public int Mods { get; set; }
	public int Count300 { get; set; }
	public int Count100 { get; set; }
	public int Count50 { get; set; }
	public int Misses { get; set; }
	public int Gekis { get; set; }
	public int Katus { get; set; }
	
	public int TotalScore { get; set; }
	public int ClassicScore { get; set; }
	public int TotalScoreWithoutMods { get; set; }
	public int LegacyTotalScore { get; set; }
	
	[Column(TypeName = "TINYINT(2)")]
	public byte Grade { get; set; }
	[Column(TypeName = "TINYINT(2)")]
	public byte Status { get; set; }
	[Column(TypeName = "TINYINT(2)")]
	public byte Mode { get; set; }
	
	[Column(TypeName = "DATETIME")]
	public DateTime PlayTime { get; set; }
	
	public int TimeElapsed { get; set; }
	public int ClientFlags { get; set; }
	public bool LegacyPerfect { get; set; }
	public bool IsPerfectCombo { get; set; }
	
	[Column(TypeName = "CHAR(32)"), Unicode(false)]
	public required string OnlineChecksum { get; set; }

	//TODO
	public bool IsRestricted { get; set; }
	
	[ForeignKey("PlayerId")]
	public PlayerDto Player { get; set; } = null!;
	public int PlayerId { get; set; }
}