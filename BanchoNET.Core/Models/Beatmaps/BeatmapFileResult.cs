namespace BanchoNET.Core.Models.Beatmaps;

public enum BeatmapFileStatus
{
	Current,
	Downloaded,
	Updated,
	Unavailable
}

/// <param name="Checksum">The checksum of the map version the local file now holds.</param>
public readonly record struct BeatmapFileResult(
	BeatmapFileStatus Status,
	string Checksum
) {
	public bool Available => Status != BeatmapFileStatus.Unavailable;
}