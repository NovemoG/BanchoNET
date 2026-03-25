using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Beatmaps;

public class Beatmap : IBeatmap,
	IEquatable<Beatmap>
{
	public BeatmapSet Set { get; set; } = null!;

	public int OnlineId => Id;
	public int Id { get; set; }
	public int SetId { get; set; }
	public bool Private { get; set; }
	public GameMode Mode { get; set; }
	public BeatmapStatus Status { get; set; }

	/// <summary>
	/// Only if map is not ranked on official server
	/// </summary>
	public DateTime NextApiCheck { get; set; } = DateTime.UtcNow.AddDays(1);
	public int ApiChecks { get; set; }
	public bool IsRankedOfficially { get; set; }
	
	public string MD5 { get; set; }
	
	public string Artist { get; set; }
	public string ArtistUnicode { get; set; }
	
	public string Title { get; set; }
	public string TitleUnicode { get; set; }
	
	public string Name { get; set; }
	public string Creator { get; set; }
	public int CreatorId { get; set; } //TODO
	
	public string Tags { get; set; }

	public DateTime SubmitDate { get; set; }
	public DateTime LastUpdate { get; set; }
	public DateTime RankedDate { get; set; }
	
	public int TotalLength { get; set; }
	public int HitLength { get; set; }
	public int MaxCombo { get; set; }
	public bool StatusFrozen { get; set; }
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
	public int SliderTailHit => SlidersCount;
	public readonly int NotesCount;
	public readonly MaxStatistics MaxStatistics;
	
	public long CoverId { get; set; }
	
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
		ArtistUnicode = apiBeatmap.ArtistUnicode;
		Title = apiBeatmap.Title;
		TitleUnicode = apiBeatmap.TitleUnicode;
		Name = apiBeatmap.Version;
		Creator = apiBeatmap.Creator;
		//TODO CreatorId = apiBeatmap.CreatorId;
		CreatorId = 1;
		Tags = apiBeatmap.Tags;
		LastUpdate = DateTime.Parse(apiBeatmap.LastUpdate);
		TotalLength = apiBeatmap.TotalLength;
		HitLength = apiBeatmap.HitLength;
		MaxCombo = apiBeatmap.MaxCombo;
		HasVideo = apiBeatmap.Video;
		HasStoryboard = apiBeatmap.Storyboard;
		Bpm = apiBeatmap.Bpm;
		Cs = apiBeatmap.DiffSize;
		Ar = apiBeatmap.DiffApproach;
		Od = apiBeatmap.DiffOverall;
		Hp = apiBeatmap.DiffDrain;
		StarRating = (float)apiBeatmap.DifficultyRating;
		NotesCount = CirclesCount + SlidersCount + SpinnersCount;
		MaxStatistics = new MaxStatistics
		{
			Great = NotesCount,
			LargeTickHit = LargeTickHit,
			IgnoreHit = IgnoreHit,
			SliderTailHit = SliderTailHit
			//TODO LegacyComboIncrease
			//	   also there are different maximum statistics depending on client
		};
		
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
		ArtistUnicode = apiBeatmap.ArtistUnicode;
		Title = apiBeatmap.Title;
		TitleUnicode = apiBeatmap.TitleUnicode;
		Name = apiBeatmap.Version;
		Creator = apiBeatmap.Creator;
		//TODO CreatorId = int.Parse(apiBeatmap.CreatorId);
		CreatorId = 1;
		Tags = apiBeatmap.Tags;
		SubmitDate = DateTime.Parse(apiBeatmap.SubmitDate);
		LastUpdate = DateTime.Parse(apiBeatmap.LastUpdate);
		TotalLength = int.Parse(apiBeatmap.TotalLength);
		HitLength = int.Parse(apiBeatmap.HitLength);
		MaxCombo = int.Parse(apiBeatmap.MaxCombo);
		HasVideo = apiBeatmap.Video == "1";
		HasStoryboard = apiBeatmap.Storyboard == "1";
		Bpm = float.Parse(apiBeatmap.Bpm);
		Cs = float.Parse(apiBeatmap.DiffSize);
		Ar = float.Parse(apiBeatmap.DiffApproach);
		Od = float.Parse(apiBeatmap.DiffOverall);
		Hp = float.Parse(apiBeatmap.DiffDrain);
		StarRating = float.Parse(apiBeatmap.DifficultyRating);
		CirclesCount = int.TryParse(apiBeatmap.CountNormal, out var circles) ? circles : 0;
		SlidersCount = int.TryParse(apiBeatmap.CountSlider, out var sliders) ? sliders : 0;
		SpinnersCount = int.TryParse(apiBeatmap.CountSpinner, out var spinners) ? spinners : 0;
		NotesCount = CirclesCount + SlidersCount + SpinnersCount;
		MaxStatistics = new MaxStatistics
		{
			Great = NotesCount,
			LargeTickHit = LargeTickHit,
			IgnoreHit = IgnoreHit,
			SliderTailHit = SliderTailHit
		};
		
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
		ArtistUnicode = beatmapDto.ArtistUnicode;
		Title = beatmapDto.Title;
		TitleUnicode = beatmapDto.TitleUnicode;
		Name = beatmapDto.Name;
		Creator = beatmapDto.CreatorName;
		//TODO CreatorId = beatmapDto.CreatorId;
		CreatorId = 1;
		Tags = beatmapDto.Tags;
		SubmitDate = beatmapDto.SubmitDate;
		LastUpdate = beatmapDto.LastUpdate;
		RankedDate = beatmapDto.RankedDate;
		TotalLength = beatmapDto.TotalLength;
		HitLength = beatmapDto.HitLength;
		MaxCombo = beatmapDto.MaxCombo;
		StatusFrozen = beatmapDto.Frozen;
		HasVideo = beatmapDto.HasVideo;
		HasStoryboard = beatmapDto.HasStoryboard;
		Plays = beatmapDto.Plays;
		Passes = beatmapDto.Passes;
		Bpm = beatmapDto.Bpm;
		Cs = beatmapDto.Cs;
		Ar = beatmapDto.Ar;
		Od = beatmapDto.Od;
		Hp = beatmapDto.Hp;
		StarRating = beatmapDto.StarRating;
		CirclesCount = beatmapDto.CirclesCount;
		SlidersCount = beatmapDto.SlidersCount;
		SpinnersCount = beatmapDto.SpinnersCount;
		IgnoreHit = beatmapDto.IgnoreHit;
		LargeTickHit = beatmapDto.LargeTickHit;
		CoverId = beatmapDto.CoverId;
		
		NotesCount = CirclesCount + SlidersCount + SpinnersCount;
		MaxStatistics = new MaxStatistics
		{
			Great = NotesCount,
			LargeTickHit = LargeTickHit,
			IgnoreHit = IgnoreHit,
			SliderTailHit = SliderTailHit
		};
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