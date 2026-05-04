using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Beatmaps;

public class Beatmap : IBeatmap,
	IEquatable<Beatmap>
{
	public Beatmapset Set { get; set; }

	public int OnlineId => Id;
	public int Id { get; set; }
	public int BeatmapsetId { get; set; }
	public string Checksum { get; set; }
	public string Version { get; set; }
	public GameMode Mode { get; set; }
	public BeatmapStatus Status { get; set; }
	public int TotalLength { get; set; }
	public int HitLength { get; set; }
	public int MaxCombo { get; set; }
	public bool IsScoreable { get; set; }
	public DateTime LastUpdated { get; set; }
	
	public float Bpm { get; set; }
	public float Cs { get; set; }
	public float Ar { get; set; }
	public float Od { get; set; }
	public float Hp { get; set; }
	public float StarRating { get; set; }
	
	public int CirclesCount { get; set; }
	public int SlidersCount { get; set; }
	public int SpinnersCount { get; set; }
	public Dictionary<HitResult, int> MaxStatistics { get; set; }
	
	public long Plays { get; set; }
	public long Passes { get; set; }
	
	public int[] Fails { get; set; } = new int[100];
	public int[] Exits { get; set; } = new int[100];

	#region Constructors

	public Beatmap(
		ApiBeatmap beatmap,
		Beatmapset set
	) {
		Set = set;
		MaxStatistics = new Dictionary<HitResult, int>();

		Id = beatmap.Id;
		BeatmapsetId = beatmap.BeatmapsetId;
		Mode = (GameMode)beatmap.ModeInt;
		Status = (BeatmapStatus)beatmap.Ranked;
		Checksum = beatmap.Checksum;
		Version = beatmap.Version;
		TotalLength = beatmap.TotalLength;
		HitLength = beatmap.HitLength;
		MaxCombo = beatmap.MaxCombo;
		IsScoreable = beatmap.IsScoreable;
		LastUpdated = beatmap.LastUpdated.DateTime;
		Bpm = (float)beatmap.Bpm;
		Cs = beatmap.Cs;
		Ar = beatmap.Ar;
		Od = beatmap.Accuracy;
		Hp = beatmap.Drain;
		StarRating = (float)beatmap.DifficultyRating;
		CirclesCount = beatmap.CountCircles;
		SlidersCount = beatmap.CountSliders;
		SpinnersCount = beatmap.CountSpinners;
	}

	public Beatmap(
		BeatmapDto beatmap,
		Beatmapset set
	) {
		Set = set;
		MaxStatistics = beatmap.MaximumStatistics;
		
		Id = beatmap.Id;
		BeatmapsetId = beatmap.SetId;
		Mode = beatmap.Mode;
		Status = beatmap.Status;
		Checksum = beatmap.MD5;
		Version = beatmap.Version;
		TotalLength = beatmap.TotalLength;
		HitLength = beatmap.HitLength;
		MaxCombo = beatmap.MaxCombo;
		IsScoreable = beatmap.IsScoreable;
		LastUpdated = beatmap.LastUpdated.DateTime;
		Bpm = beatmap.Bpm;
		Cs = beatmap.Cs;
		Ar = beatmap.Ar;
		Od = beatmap.Od;
		Hp = beatmap.Hp;
		StarRating = beatmap.StarRating;
		CirclesCount = beatmap.CirclesCount;
		SlidersCount = beatmap.SlidersCount;
		SpinnersCount = beatmap.SpinnersCount;
		
		Plays = beatmap.Plays;
		Passes = beatmap.Passes;
		Fails = beatmap.Fails;
		Exits = beatmap.Exits;
	}

	#endregion

	#region IEquatable

	public bool Equals(Beatmap? other) => this.MatchesOnlineId(other);
    
	public override bool Equals(
		object? obj
	) {
		return ReferenceEquals(this, obj) || obj is Beatmap other && Equals(other);
	}
	public override int GetHashCode() => OnlineId.GetHashCode();

	#endregion
}