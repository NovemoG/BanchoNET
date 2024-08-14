using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[Index(nameof(MapId))]
[Index(nameof(SetId))]
[Index(nameof(MD5), IsUnique = true)]
[PrimaryKey(nameof(MapId))]
public class BeatmapDto
{
	[Key] public int MapId { get; set; }
	public int SetId { get; set; }
	public bool Private { get; set; }
	
	public bool IsRankedOfficially { get; set; }
	
	public byte Mode { get; set; }
	public sbyte Status { get; set; }
	
	[Column(TypeName = "CHAR(32)"), Unicode(false)]
	public required string MD5 { get; set; }
	
	[MaxLength(128), Unicode(false)]
	public string Artist { get; set; }
	[MaxLength(128), Unicode(false)]
	public string Title { get; set; }
	[MaxLength(128), Unicode(false)]
	public string Name { get; set; }
	[MaxLength(16), Unicode(false)]
	public string Creator { get; set; }
	
	[Column(TypeName = "DATETIME")]
	public DateTime SubmitDate { get; set; }
	[Column(TypeName = "DATETIME")]
	public DateTime LastUpdate { get; set; }
	
	public int TotalLength { get; set; }
	public int MaxCombo { get; set; }
	public bool Frozen { get; set; }
	public bool HasVideo { get; set; }
	public long Plays { get; set; }
	public long Passes { get; set; }
	
	[Column(TypeName = "FLOAT(15,3)")] public float Bpm { get; set; }
	[Column(TypeName = "FLOAT(4,2)")] public float Cs { get; set; }
	[Column(TypeName = "FLOAT(4,2)")] public float Ar { get; set; }
	[Column(TypeName = "FLOAT(4,2)")] public float Od { get; set; }
	[Column(TypeName = "FLOAT(4,2)")] public float Hp { get; set; }
	[Column(TypeName = "FLOAT(9,3)")] public float StarRating { get; set; }
	
	public int NotesCount { get; set; }
	public int SlidersCount { get; set; }
	public int SpinnersCount { get; set; }
}