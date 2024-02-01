namespace BanchoNET.Objects.Beatmaps;

public enum BeatmapStatus
{
	Unknown = -2,
	NotSubmitted = -1,
	LatestPending = 0,
	NeedsUpdate = 1,
	Ranked = 2,
	Approved = 3,
	Qualified = 4,
	Loved = 5,
}