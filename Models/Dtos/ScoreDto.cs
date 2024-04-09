using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Models.Dtos;

[Index(nameof(PP))]
[Index(nameof(Score))]
[PrimaryKey(nameof(Id))]
public class ScoreDto
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long Id { get; set; }
	
	[MaxLength(16), Unicode(false)]
	public string Username { get; set; }
	
	[Column(TypeName = "CHAR(2)"), Unicode(false)]
	public string CountryCode { get; set; } = null!;
	
	[Column(TypeName = "CHAR(32)"), Unicode(false)]
	public string BeatmapMD5 { get; set; }
	
	[Column(TypeName = "FLOAT(7,3)")]
	public float PP { get; set; }
	[Column(TypeName = "FLOAT(6,3)")]
	public float Acc { get; set; }
	public int Score { get; set; }
	public int MaxCombo { get; set; }
	public int Mods { get; set; }
	public int Count300 { get; set; }
	public int Count100 { get; set; }
	public int Count50 { get; set; }
	public int Misses { get; set; }
	public int Gekis { get; set; }
	public int Katus { get; set; }
	
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
	public bool Perfect { get; set; }
	
	[Column(TypeName = "CHAR(32)"), Unicode(false)]
	public string OnlineChecksum { get; set; }
	
	[ForeignKey("PlayerId")]
	public PlayerDto Player { get; set; } = null!;
	public int PlayerId { get; set; }
	
	public bool IsRestricted { get; set; }
}