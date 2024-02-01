using BanchoNET.Objects.Beatmaps;

namespace BanchoNET.Utils;

public static class Beatmaps
{
	public static string FullName(this Beatmap beatmap)
	{
		return $"{beatmap.Artist} - {beatmap.Title} [{beatmap.DiffName}]";
	}
}