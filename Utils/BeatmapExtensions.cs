using System.Security.Cryptography;
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

	public static bool CheckLocalBeatmapMD5(this string beatmapFile, string beatmapMD5)
	{
		using var md5 = MD5.Create();
		using var stream = File.OpenRead(beatmapFile);
		
		var hash = md5.ComputeHash(stream);
		var md5String = Convert.ToHexString(hash).ToLower();
		
		Console.WriteLine($"[BeatmapExtensions] {md5String}, {beatmapMD5}");

		return md5String == beatmapMD5;
	}
}