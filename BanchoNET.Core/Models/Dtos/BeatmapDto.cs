using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Models.Dtos;

public class BeatmapDto
{
	public int Id { get; set; }
	public int SetId { get; set; }
	public required string MD5 { get; set; }
	
	public string Version { get; set; }
	public GameMode Mode { get; set; }
	public BeatmapStatus Status { get; set; }
	public float StarRating { get; set; }
	public float Bpm { get; set; }
	public float Cs { get; set; }
	public float Ar { get; set; }
	public float Od { get; set; }
	public float Hp { get; set; }
	public int CirclesCount { get; set; }
	public int SlidersCount { get; set; }
	public int SpinnersCount { get; set; }
	public int MaxCombo { get; set; }
	public int TotalLength { get; set; }
	public int HitLength { get; set; }
	public bool IsScoreable { get; set; }
	public DateTimeOffset LastUpdated { get; set; }
	public long Plays { get; set; }
	public long Passes { get; set; }
	
	public int OwnerId { get; set; }
	public string OwnerName { get; set; } = "";

	/// <summary>
	/// JSON value of MaximumStatistics
	/// <remarks>Should be populated if found empty</remarks>
	/// </summary>
	public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new();
	
	public int[] Fails { get; set; } = new int[100];
	public int[] Exits { get; set; } = new int[100];
	
	public ICollection<BeatmapCollaborator> Collaborators { get; set; } = [];
	public ICollection<BeatmapPlays> PlaysData { get; set; } = [];
	public ICollection<ScoreDto> Scores { get; set; } = [];
	public BeatmapsetDto Beatmapset { get; set; } = null!;
	
	public long SkillsId { get; set; }
	public SkillsDto Skills { get; set; } = new();
}