namespace BanchoNET.Core.Models.Beatmaps;

public enum BeatmapStatus : sbyte
{
	Graveyard = -2,
	WIP = -1,
	LatestPending = 0,
	Ranked = 1,
	Approved = 2,
	Qualified = 3,
	Loved = 4,
}