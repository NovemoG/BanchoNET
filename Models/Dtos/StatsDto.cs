using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[PrimaryKey(nameof(PlayerId), nameof(Mode))]
public class StatsDto
{
	[Key, ForeignKey("Player")]
	public int PlayerId { get; set; }
	[Key] public byte Mode { get; set; }
	
	public long TotalScore { get; set; }
	public long RankedScore { get; set; }
	public ushort PP { get; set; }
	
	[Column(TypeName = "FLOAT(6,3)")]
	public float Accuracy { get; set; }
	
	public int PlayCount { get; set; }
	public int PlayTime { get; set; }
	public int MaxCombo { get; set; }
	public int TotalGekis { get; set; }
	public int TotalKatus { get; set; }
	public int Total300s { get; set; }
	public int Total100s { get; set; }
	public int Total50s { get; set; }
	public int ReplaysViews { get; set; }
	public int XHCount { get; set; }
	public int XCount { get; set; }
	public int SHCount { get; set; }
	public int SCount { get; set; }
	public int ACount { get; set; }
}