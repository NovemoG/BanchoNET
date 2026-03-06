using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

[PrimaryKey(nameof(PlayerId), nameof(Mode))]
public class StatsDto
{
	[ForeignKey("PlayerId")]
	public PlayerDto Player { get; set; } = null!;
	[Key] public int PlayerId { get; set; }
	[Key] public byte Mode { get; set; }
	
	public long TotalScore { get; set; }
	public long RankedScore { get; set; }
	public int PP { get; set; }

	public bool IsRanked { get; set; } = true; //TODO whether player is seen in global ranking
	
	[Column(TypeName = "FLOAT(6,3)")]
	public float Accuracy { get; set; }
	
	public int PeakRank { get; set; }
	public int PlayCount { get; set; }
	public int PlayTime { get; set; }
	public int MaxCombo { get; set; }
	public long TotalGekis { get; set; }
	public long TotalKatus { get; set; }
	public long Total300s { get; set; }
	public long Total100s { get; set; }
	public long Total50s { get; set; }
	public long TotalMisses { get; set; }
	public int ReplayViews { get; set; }
	public int XHCount { get; set; }
	public int XCount { get; set; }
	public int SHCount { get; set; }
	public int SCount { get; set; }
	public int ACount { get; set; }
	
	[NotMapped] public long TotalStdHits => Total300s + Total100s + Total50s;
	[NotMapped] public long TotalTaikoHits => TotalStdHits + TotalGekis + TotalKatus;
	[NotMapped] public long TotalManiaHits => TotalTaikoHits;
	[NotMapped] public long TotalCatchHits => TotalStdHits;
}