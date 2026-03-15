using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BanchoNET.Core.Models.Dtos;

public class BeatmapDto
{
	public int MapId { get; set; }
	public int SetId { get; set; }
	public bool Private { get; set; }
	
	public bool IsRankedOfficially { get; set; }
	
	public byte Mode { get; set; }
	public sbyte Status { get; set; }
	
	public required string MD5 { get; set; }
	
	public string Artist { get; set; }
	public string ArtistUnicode { get; set; }
	
	public string Title { get; set; }
	public string TitleUnicode { get; set; }
	
	public string Name { get; set; }
	public string CreatorName { get; set; }
	public int CreatorId { get; set; }
	
	public string Tags { get; set; }
	
	public DateTime SubmitDate { get; set; }
	public DateTime LastUpdate { get; set; }
	public DateTime RankedDate { get; set; }
	
	public int TotalLength { get; set; }
	public int HitLength { get; set; }
	public int MaxCombo { get; set; }
	public bool Frozen { get; set; }
	public bool HasVideo { get; set; }
	public bool HasStoryboard { get; set; }
	public long Plays { get; set; }
	public long Passes { get; set; }
	
	public float Bpm { get; set; }
	public float Cs { get; set; }
	public float Ar { get; set; }
	public float Od { get; set; }
	public float Hp { get; set; }
	public float StarRating { get; set; }
	
	public int CirclesCount { get; set; }
	public int SlidersCount { get; set; }
	public int SpinnersCount { get; set; }
	public int IgnoreHit { get; set; }
	public int LargeTickHit { get; set; }
	[NotMapped] public int SliderTailHit => SlidersCount;
	[NotMapped] public int NotesCount => CirclesCount + SlidersCount + SpinnersCount;
	
	public long CoverId { get; set; } //TODO move common fields to a beatmapset dto
	
	[ForeignKey(nameof(CreatorId))]
	public PlayerDto Creator { get; set; } = null!;
	
	[ForeignKey(nameof(SetId))]
	public BeatmapsetDto Beatmapset { get; set; } = null!;
}