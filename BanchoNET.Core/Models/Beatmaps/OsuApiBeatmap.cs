using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Beatmaps;

public class OsuApiBeatmap
{
	[JsonPropertyName("beatmapset_id")] public string BeatmapsetId { get; set; }

	[JsonPropertyName("beatmap_id")] public string BeatmapId { get; set; }

	[JsonPropertyName("approved")] public string Approved { get; set; }

	[JsonPropertyName("total_length")] public string TotalLength { get; set; }

	[JsonPropertyName("hit_length")] public string HitLength { get; set; }

	[JsonPropertyName("version")] public string Version { get; set; }

	[JsonPropertyName("file_md5")] public string FileMd5 { get; set; }

	[JsonPropertyName("diff_size")] public string DiffSize { get; set; }

	[JsonPropertyName("diff_overall")] public string DiffOverall { get; set; }

	[JsonPropertyName("diff_approach")] public string DiffApproach { get; set; }

	[JsonPropertyName("diff_drain")] public string DiffDrain { get; set; }

	[JsonPropertyName("mode")] public string Mode { get; set; }

	[JsonPropertyName("count_normal")] public string CountNormal { get; set; }

	[JsonPropertyName("count_slider")] public string CountSlider { get; set; }

	[JsonPropertyName("count_spinner")] public string CountSpinner { get; set; }

	[JsonPropertyName("submit_date")] public string SubmitDate { get; set; }

	[JsonPropertyName("approved_date")] public string ApprovedDate { get; set; }

	[JsonPropertyName("last_update")] public string LastUpdate { get; set; }

	[JsonPropertyName("artist")] public string Artist { get; set; }

	[JsonPropertyName("artist_unicode")] public string ArtistUnicode { get; set; }

	[JsonPropertyName("title")] public string Title { get; set; }

	[JsonPropertyName("title_unicode")] public string TitleUnicode { get; set; }

	[JsonPropertyName("creator")] public string Creator { get; set; }

	[JsonPropertyName("creator_id")] public string CreatorId { get; set; }

	[JsonPropertyName("bpm")] public string Bpm { get; set; }

	[JsonPropertyName("source")] public string Source { get; set; }

	[JsonPropertyName("tags")] public string Tags { get; set; }

	[JsonPropertyName("genre_id")] public string GenreId { get; set; }

	[JsonPropertyName("language_id")] public string LanguageId { get; set; }

	[JsonPropertyName("favourite_count")] public string FavouriteCount { get; set; }

	[JsonPropertyName("rating")] public string Rating { get; set; }

	[JsonPropertyName("storyboard")] public string Storyboard { get; set; }

	[JsonPropertyName("video")] public string Video { get; set; }

	[JsonPropertyName("download_unavailable")] public string DownloadUnavailable { get; set; }

	[JsonPropertyName("audio_unavailable")] public string AudioUnavailable { get; set; }

	[JsonPropertyName("playcount")] public string Playcount { get; set; }

	[JsonPropertyName("passcount")] public string Passcount { get; set; }

	[JsonPropertyName("packs")] public object Packs { get; set; }

	[JsonPropertyName("max_combo")] public string MaxCombo { get; set; }

	[JsonPropertyName("diff_aim")] public string DiffAim { get; set; }

	[JsonPropertyName("diff_speed")] public string DiffSpeed { get; set; }

	[JsonPropertyName("difficultyrating")] public string DifficultyRating { get; set; }
}