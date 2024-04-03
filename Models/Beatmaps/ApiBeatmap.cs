using Newtonsoft.Json;

namespace BanchoNET.Models.Beatmaps;

public class ApiBeatmap
{
	[JsonProperty("beatmapset_id")] public int BeatmapsetId { get; set; }

	[JsonProperty("beatmap_id")] public int BeatmapId { get; set; }

	[JsonProperty("approved")] public int Approved { get; set; }

	[JsonProperty("total_length")] public int TotalLength { get; set; }

	[JsonProperty("hit_length")] public int HitLength { get; set; }

	[JsonProperty("version")] public string Version { get; set; }

	[JsonProperty("file_md5")] public string FileMd5 { get; set; }

	[JsonProperty("diff_size")] public float DiffSize { get; set; }

	[JsonProperty("diff_overall")] public float DiffOverall { get; set; }

	[JsonProperty("diff_approach")] public float DiffApproach { get; set; }

	[JsonProperty("diff_drain")] public float DiffDrain { get; set; }

	[JsonProperty("mode")] public int Mode { get; set; }

	[JsonProperty("approved_date")] public object ApprovedDate { get; set; }

	[JsonProperty("last_update")] public string LastUpdate { get; set; }

	[JsonProperty("artist")] public string Artist { get; set; }

	[JsonProperty("artist_unicode")] public string ArtistUnicode { get; set; }

	[JsonProperty("title")] public string Title { get; set; }

	[JsonProperty("title_unicode")] public string TitleUnicode { get; set; }

	[JsonProperty("creator")] public string Creator { get; set; }

	[JsonProperty("creator_id")] public int CreatorId { get; set; }

	[JsonProperty("bpm")] public int Bpm { get; set; }

	[JsonProperty("source")] public string Source { get; set; }

	[JsonProperty("tags")] public string Tags { get; set; }

	[JsonProperty("genre_id")] public int GenreId { get; set; }

	[JsonProperty("language_id")] public int LanguageId { get; set; }

	[JsonProperty("favourite_count")] public int FavouriteCount { get; set; }

	[JsonProperty("storyboard")] public bool Storyboard { get; set; }

	[JsonProperty("video")] public bool Video { get; set; }

	[JsonProperty("download_unavailable")] public bool DownloadUnavailable { get; set; }

	[JsonProperty("playcount")] public int Playcount { get; set; }

	[JsonProperty("passcount")] public int Passcount { get; set; }

	[JsonProperty("packs")] public string[]? Packs { get; set; }

	[JsonProperty("max_combo")] public int MaxCombo { get; set; }

	[JsonProperty("difficultyrating")] public double DifficultyRating { get; set; }
}