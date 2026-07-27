using System.ComponentModel.DataAnnotations.Schema;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;

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
	public LegacyMods Mods { get; set; }
	
	/// <summary>
	/// Mods are stored as an ordered string of keys separated by semicolon
	/// </summary>
	public required string ModKeys { get; set; }
	/// <summary>
	/// All mods as a string value
	/// </summary>
	public string? LazerMods { get; set; }
	
	/// <summary>
	/// JSON value of Statistics
	/// </summary>
	public Dictionary<HitResult, int> Statistics { get; set; } = new();
	
	public long TotalScore { get; set; }
	public long TotalScoreWithoutMods { get; set; }
	
	//TODO this should stay int and TotalScore should be used instead 
	public long LegacyTotalScore { get; set; }
	
	public Grade Grade { get; set; }
	public SubmissionStatus Status { get; set; }
	public GameMode Mode { get; set; }
	
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
	
	public PlayerDto Player { get; set; } = null!;
	public int PlayerId { get; set; }
	
	public BeatmapDto Beatmap { get; set; } = null!;
	
	public ICollection<ReplayWatches> ReplayWatches { get; set; } = [];
}