namespace BanchoNET.Core.Models.Dtos;

public enum BeatmapsetDownloadType
{
	All,
	NoVideo,
	Direct
}

public class PlayerProfileCustomizationDto
{
	public int PlayerId { get; set; }

	public BeatmapsetDownloadType BeatmapsetDownload { get; set; } = BeatmapsetDownloadType.All;
	public bool BeatmapsetShowNsfw { get; set; }
	public bool BeatmapsetShowAnimeCover { get; set; } = true;
	public bool BeatmapsetTitleShowOriginal { get; set; }

	public int? ProfileHue { get; set; }
	public string? ExtrasOrder { get; set; }

	public DateTime UpdatedAt { get; set; }

	public PlayerDto Player { get; set; } = null!;
}