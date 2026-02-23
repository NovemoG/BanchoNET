using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Beatmaps;

public class ApiBeatmap
{
	[JsonPropertyName("beatmapset_id")] public int BeatmapsetId { get; set; }

	[JsonPropertyName("beatmap_id")] public int BeatmapId { get; set; }

	[JsonPropertyName("approved")] public int Approved { get; set; }

	[JsonPropertyName("total_length")] public int TotalLength { get; set; }

	[JsonPropertyName("hit_length")] public int HitLength { get; set; }

	[JsonPropertyName("version")] public string Version { get; set; }

	[JsonPropertyName("file_md5")] public string FileMd5 { get; set; }

	[JsonPropertyName("diff_size")] public float DiffSize { get; set; }

	[JsonPropertyName("diff_overall")] public float DiffOverall { get; set; }

	[JsonPropertyName("diff_approach")] public float DiffApproach { get; set; }

	[JsonPropertyName("diff_drain")] public float DiffDrain { get; set; }

	[JsonPropertyName("mode")] public int Mode { get; set; }

	[JsonPropertyName("approved_date")] public object ApprovedDate { get; set; }

	[JsonPropertyName("last_update")] public string LastUpdate { get; set; }

	[JsonPropertyName("artist")] public string Artist { get; set; }

	[JsonPropertyName("artist_unicode")] public string ArtistUnicode { get; set; }

	[JsonPropertyName("title")] public string Title { get; set; }

	[JsonPropertyName("title_unicode")] public string TitleUnicode { get; set; }

	[JsonPropertyName("creator")] public string Creator { get; set; }

	[JsonPropertyName("creator_id")] public int CreatorId { get; set; }

	[JsonPropertyName("bpm")] public int Bpm { get; set; }

	[JsonPropertyName("source")] public string Source { get; set; }

	[JsonPropertyName("tags")] public string Tags { get; set; }

	[JsonPropertyName("genre_id")] public int GenreId { get; set; }

	[JsonPropertyName("language_id")] public int LanguageId { get; set; }

	[JsonPropertyName("favourite_count")] public int FavouriteCount { get; set; }

	[JsonPropertyName("storyboard")] public bool Storyboard { get; set; }

	[JsonPropertyName("video")] public bool Video { get; set; }

	[JsonPropertyName("download_unavailable")] public bool DownloadUnavailable { get; set; }

	[JsonPropertyName("playcount")] public int Playcount { get; set; }

	[JsonPropertyName("passcount")] public int Passcount { get; set; }

	[JsonPropertyName("packs")] public string[]? Packs { get; set; }

	[JsonPropertyName("max_combo")] public int MaxCombo { get; set; }

	[JsonPropertyName("difficultyrating")] public double DifficultyRating { get; set; }
}