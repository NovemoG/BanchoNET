using Newtonsoft.Json;

namespace BanchoNET.Models.Beatmaps;

public class OsuApiBeatmap
{
	[JsonProperty("beatmapset_id")] public string BeatmapsetId { get; set; }

	[JsonProperty("beatmap_id")] public string BeatmapId { get; set; }

	[JsonProperty("approved")] public string Approved { get; set; }

	[JsonProperty("total_length")] public string TotalLength { get; set; }

	[JsonProperty("hit_length")] public string HitLength { get; set; }

	[JsonProperty("version")] public string Version { get; set; }

	[JsonProperty("file_md5")] public string FileMd5 { get; set; }

	[JsonProperty("diff_size")] public string DiffSize { get; set; }

	[JsonProperty("diff_overall")] public string DiffOverall { get; set; }

	[JsonProperty("diff_approach")] public string DiffApproach { get; set; }

	[JsonProperty("diff_drain")] public string DiffDrain { get; set; }

	[JsonProperty("mode")] public string Mode { get; set; }

	[JsonProperty("count_normal")] public string CountNormal { get; set; }

	[JsonProperty("count_slider")] public string CountSlider { get; set; }

	[JsonProperty("count_spinner")] public string CountSpinner { get; set; }

	[JsonProperty("submit_date")] public string SubmitDate { get; set; }

	[JsonProperty("approved_date")] public string ApprovedDate { get; set; }

	[JsonProperty("last_update")] public string LastUpdate { get; set; }

	[JsonProperty("artist")] public string Artist { get; set; }

	[JsonProperty("artist_unicode")] public string ArtistUnicode { get; set; }

	[JsonProperty("title")] public string Title { get; set; }

	[JsonProperty("title_unicode")] public string TitleUnicode { get; set; }

	[JsonProperty("creator")] public string Creator { get; set; }

	[JsonProperty("creator_id")] public string CreatorId { get; set; }

	[JsonProperty("bpm")] public string Bpm { get; set; }

	[JsonProperty("source")] public string Source { get; set; }

	[JsonProperty("tags")] public string Tags { get; set; }

	[JsonProperty("genre_id")] public string GenreId { get; set; }

	[JsonProperty("language_id")] public string LanguageId { get; set; }

	[JsonProperty("favourite_count")] public string FavouriteCount { get; set; }

	[JsonProperty("rating")] public string Rating { get; set; }

	[JsonProperty("storyboard")] public string Storyboard { get; set; }

	[JsonProperty("video")] public string Video { get; set; }

	[JsonProperty("download_unavailable")] public string DownloadUnavailable { get; set; }

	[JsonProperty("audio_unavailable")] public string AudioUnavailable { get; set; }

	[JsonProperty("playcount")] public string Playcount { get; set; }

	[JsonProperty("passcount")] public string Passcount { get; set; }

	[JsonProperty("packs")] public object Packs { get; set; }

	[JsonProperty("max_combo")] public string MaxCombo { get; set; }

	[JsonProperty("diff_aim")] public string DiffAim { get; set; }

	[JsonProperty("diff_speed")] public string DiffSpeed { get; set; }

	[JsonProperty("difficultyrating")] public string DifficultyRating { get; set; }
}