using BanchoNET.Objects.Beatmaps;

namespace BanchoNET.Utils;

public static class BeatmapExtensions
{
	public static string FullName(this Beatmap beatmap)
	{
		return $"{beatmap.Artist} - {beatmap.Title} [{beatmap.Name}]";
	}

	public static bool HasLeaderboard(this Beatmap beatmap)
	{
		return beatmap.Status is BeatmapStatus.Approved or BeatmapStatus.Ranked or BeatmapStatus.Loved;
	}

	public static bool AwardsPP(this Beatmap beatmap)
	{
		return beatmap.Status is BeatmapStatus.Approved or BeatmapStatus.Ranked;
	}
}