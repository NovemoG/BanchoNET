using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Beatmaps;

public class Beatmap : IBeatmap,
	IEquatable<Beatmap>
{
	public BeatmapSet? Set { get; set; }

	public int OnlineId => Id;
	public int Id { get; set; }
	public int SetId { get; set; }
	public bool Private { get; set; }
	public GameMode Mode { get; set; }
	public BeatmapStatus Status { get; set; }

	/// <summary>
	/// Only if map is not ranked on official server
	/// </summary>
	public DateTime NextApiCheck { get; set; } = DateTime.UtcNow.Add(TimeSpan.FromDays(1));
	public int ApiChecks { get; set; }
	public bool IsRankedOfficially { get; set; }
	
	public string MD5 { get; set; }
	
	public string Artist { get; set; }
	public string Title { get; set; }
	public string Name { get; set; }
	public string Creator { get; set; }

	public DateTime SubmitDate { get; set; }
	public DateTime LastUpdate { get; set; }
	public int TotalLength { get; set; }
	public int MaxCombo { get; set; }
	public bool StatusFrozen { get; set; }
	public bool HasVideo { get; set; }
	public long Plays { get; set; }
	public long Passes { get; set; }
	
	public float Bpm { get; set; }
	public float Cs { get; set; }
	public float Ar { get; set; }
	public float Od { get; set; }
	public float Hp { get; set; }
	public float StarRating { get; set; }
	
	public int NotesCount { get; set; }
	public int SlidersCount { get; set; }
	public int SpinnersCount { get; set; }
	
	//TODO ToString

	#region Constructors

	public Beatmap(ApiBeatmap apiBeatmap)
	{
		Id = apiBeatmap.BeatmapId;
		SetId = apiBeatmap.BeatmapsetId;
		Private = apiBeatmap.DownloadUnavailable;
		Mode = (GameMode)apiBeatmap.Mode;
		Status = apiBeatmap.Approved.StatusFromApi(StatusFrozen, Status);
		MD5 = apiBeatmap.FileMd5;
		Artist = apiBeatmap.Artist;
		Title = apiBeatmap.Title;
		Name = apiBeatmap.Version;
		Creator = apiBeatmap.Creator;
		LastUpdate = DateTime.Parse(apiBeatmap.LastUpdate);
		TotalLength = apiBeatmap.TotalLength;
		MaxCombo = apiBeatmap.MaxCombo;
		HasVideo = apiBeatmap.Video;
		Bpm = apiBeatmap.Bpm;
		Cs = apiBeatmap.DiffSize;
		Ar = apiBeatmap.DiffApproach;
		Od = apiBeatmap.DiffOverall;
		Hp = apiBeatmap.DiffDrain;
		StarRating = (float)apiBeatmap.DifficultyRating;
		
		IsRankedOfficially = Status is BeatmapStatus.Ranked or BeatmapStatus.Approved;
	}

	public Beatmap(OsuApiBeatmap apiBeatmap)
	{
		Id = int.Parse(apiBeatmap.BeatmapId);
		SetId = int.Parse(apiBeatmap.BeatmapsetId);
		Private = apiBeatmap.DownloadUnavailable == "1";
		Mode = (GameMode)int.Parse(apiBeatmap.Mode);
		Status = int.Parse(apiBeatmap.Approved).StatusFromApi(StatusFrozen, Status);
		MD5 = apiBeatmap.FileMd5;
		Artist = apiBeatmap.Artist;
		Title = apiBeatmap.Title;
		Name = apiBeatmap.Version;
		Creator = apiBeatmap.Creator;
		SubmitDate = DateTime.Parse(apiBeatmap.SubmitDate);
		LastUpdate = DateTime.Parse(apiBeatmap.LastUpdate);
		TotalLength = int.Parse(apiBeatmap.TotalLength);
		MaxCombo = int.Parse(apiBeatmap.MaxCombo);
		HasVideo = apiBeatmap.Video == "1";
		Bpm = float.Parse(apiBeatmap.Bpm);
		Cs = float.Parse(apiBeatmap.DiffSize);
		Ar = float.Parse(apiBeatmap.DiffApproach);
		Od = float.Parse(apiBeatmap.DiffOverall);
		Hp = float.Parse(apiBeatmap.DiffDrain);
		StarRating = float.Parse(apiBeatmap.DifficultyRating);
		NotesCount = int.Parse(apiBeatmap.CountNormal);
		SlidersCount = int.Parse(apiBeatmap.CountSlider);
		SpinnersCount = int.Parse(apiBeatmap.CountSpinner);
		
		IsRankedOfficially = Status is BeatmapStatus.Ranked or BeatmapStatus.Approved;
	}

	public Beatmap(BeatmapDto beatmapDto)
	{
		Id = beatmapDto.MapId;
		SetId = beatmapDto.SetId;
		Private = beatmapDto.Private;
		Mode = (GameMode)beatmapDto.Mode;
		Status = (BeatmapStatus)beatmapDto.Status;
		IsRankedOfficially = beatmapDto.IsRankedOfficially;
		MD5 = beatmapDto.MD5;
		Artist = beatmapDto.Artist;
		Title = beatmapDto.Title;
		Name = beatmapDto.Name;
		Creator = beatmapDto.Creator;
		SubmitDate = beatmapDto.SubmitDate;
		LastUpdate = beatmapDto.LastUpdate;
		TotalLength = beatmapDto.TotalLength;
		MaxCombo = beatmapDto.MaxCombo;
		StatusFrozen = beatmapDto.Frozen;
		HasVideo = beatmapDto.HasVideo;
		Plays = beatmapDto.Plays;
		Passes = beatmapDto.Passes;
		Bpm = beatmapDto.Bpm;
		Cs = beatmapDto.Cs;
		Ar = beatmapDto.Ar;
		Od = beatmapDto.Od;
		Hp = beatmapDto.Hp;
		StarRating = beatmapDto.StarRating;
		NotesCount = beatmapDto.NotesCount;
		SlidersCount = beatmapDto.SlidersCount;
		SpinnersCount = beatmapDto.SpinnersCount;
	}

	#endregion

	#region IEquatable

	public bool Equals(Beatmap? other) => this.MatchesOnlineId(other);
    
	public override bool Equals(
		object? obj
	) {
		return ReferenceEquals(this, obj) || obj is Beatmap other && Equals(other);
	}
	public override int GetHashCode() => Id.GetHashCode();

	#endregion
}