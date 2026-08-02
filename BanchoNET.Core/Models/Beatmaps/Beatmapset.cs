using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Beatmaps;

public class Beatmapset : IBeatmapSet,
	IEquatable<Beatmapset>
{
	public int OnlineId => Id;
	public int Id { get; set; }
	public List<Beatmap> Beatmaps { get; }
	
	public bool IsPrivateUpload { get; set; }
	public bool IsRankedOfficially { get; set; }
	public bool Nsfw { get; set; }
	
	public string Artist { get; set; }
	public string ArtistUnicode { get; set; }
	
	public string Title { get; set; }
	public string TitleUnicode { get; set; }
	
	public string CreatorName { get; set; }
	public int CreatorId { get; set; }
	
	public string Tags { get; set; }
	public string Description { get; set; }
	
	public DateTime SubmitDate { get; set; }
	public DateTime LastUpdate { get; set; }
	public DateTime? RankedDate { get; set; }
	
	public BeatmapStatus Status { get; set; }
	public int FavoriteCount { get; set; }
	public int PlayCount { get; set; }
	public string Source { get; set; }
	public bool Video { get; set; }
	public bool Storyboard { get; set; }
	public int GenreId { get; set; }
	public int LanguageId { get; set; }
	public float Bpm { get; set; }
	public bool IsScoreable { get; set; }
	
	public float Rating { get; set; }
	public int[] Ratings { get; set; } = new int[10];
	
	/// <summary>
	/// Only if map is not ranked on official server
	/// </summary>
	public DateTime NextApiCheck { get; set; } = DateTime.UtcNow.AddDays(1);
	public int ApiChecks { get; set; }

	#region Constructors

	public Beatmapset(
		ApiBeatmapsetFull beatmapset
	) {
		Id = beatmapset.Id;
		Beatmaps = beatmapset.Beatmaps.Select(b => new Beatmap(b, this)).ToList();

		IsPrivateUpload = false;
		IsRankedOfficially = (BeatmapStatus)beatmapset.Ranked == BeatmapStatus.Ranked;
		Nsfw = beatmapset.Nsfw;
		Artist = beatmapset.Artist;
		ArtistUnicode = beatmapset.ArtistUnicode;
		Title = beatmapset.Title;
		TitleUnicode = beatmapset.TitleUnicode;
		CreatorName = beatmapset.Creator;
		CreatorId = 1;
		Tags = beatmapset.Tags;
		Description = beatmapset.Description.Description;
		SubmitDate = beatmapset.SubmittedDate.DateTime;
		LastUpdate = beatmapset.LastUpdated.DateTime;
		RankedDate = beatmapset.RankedDate?.DateTime;
		Status = (BeatmapStatus)beatmapset.Ranked;
		Source = beatmapset.Source;
		Video = beatmapset.Video;
		Storyboard = beatmapset.Storyboard;
		GenreId = beatmapset.GenreId;
		LanguageId = beatmapset.LanguageId;
		Bpm = (float)beatmapset.Bpm;
		IsScoreable = beatmapset.IsScoreable;
	}

	public Beatmapset(
		BeatmapsetDto beatmapset
	) {
		Id = beatmapset.Id;
		Beatmaps = beatmapset.Beatmaps.Select(b => new Beatmap(b, this)).ToList();
		
		IsPrivateUpload = beatmapset.IsPrivateUpload;
		IsRankedOfficially = beatmapset.IsRankedOfficially;
		Nsfw = beatmapset.Nsfw;
		Artist = beatmapset.Artist;
		ArtistUnicode = beatmapset.ArtistUnicode;
		Title = beatmapset.Title;
		TitleUnicode = beatmapset.TitleUnicode;
		CreatorName = beatmapset.CreatorName;
		CreatorId = beatmapset.CreatorId;
		Tags = beatmapset.Tags;
		Description = beatmapset.Description;
		SubmitDate = beatmapset.SubmittedDate.DateTime;
		LastUpdate = beatmapset.LastUpdated.DateTime;
		RankedDate = beatmapset.RankedDate?.DateTime;
		Status = beatmapset.Status;
		Source = beatmapset.Source;
		Video = beatmapset.Video;
		Storyboard = beatmapset.Storyboard;
		GenreId = beatmapset.GenreId;
		LanguageId = beatmapset.LanguageId;
		Bpm = beatmapset.Bpm;
		IsScoreable = beatmapset.IsScoreable;
		FavoriteCount = beatmapset.FavoriteCount;
		PlayCount = beatmapset.PlayCount;
		Ratings = beatmapset.Ratings;
		Rating = beatmapset.Rating;
	}

	#endregion

	#region IEquatable

	public bool Equals(Beatmapset? other) => this.MatchesOnlineId(other);
    
	public override bool Equals(
		object? obj
	) {
		return ReferenceEquals(this, obj) || obj is Beatmapset other && Equals(other);
	}
	public override int GetHashCode() => OnlineId.GetHashCode();

	#endregion
}