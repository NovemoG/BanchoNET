using System.Security.Cryptography;
using BanchoNET.Objects.Beatmaps;

namespace BanchoNET.Utils;

public static class BeatmapExtensions
{
	private static string _url = null!;
	
	public static void InitBaseUrlValue(string domain)
	{
		_url = $"https://osu.{domain}";
	}
	
	public static string FullName(this Beatmap beatmap)
	{
		return $"{beatmap.Artist} - {beatmap.Title} [{beatmap.Name}]";
	}

	public static string Url(this Beatmap beatmap)
	{
		return $"{_url}/b/{beatmap.MapId}";
	}

	public static string Url(this BeatmapSet set)
	{
		return $"{_url}/s/{set.Id}";
	}

	public static string Embed(this Beatmap beatmap)
	{
		return $"[{beatmap.Url} {beatmap.FullName}]";
	}

	public static bool HasLeaderboard(this Beatmap beatmap)
	{
		return beatmap.Status is
			BeatmapStatus.Approved or
			BeatmapStatus.Ranked or
			BeatmapStatus.Loved or
			BeatmapStatus.Qualified;
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

		return md5String == beatmapMD5;
	}

	public static BeatmapStatus StatusFromDirectApi(
		this int status,
		bool frozen,
		BeatmapStatus prevStatus)
	{
		if (frozen) return prevStatus;
		
		return status switch
		{
			1 => BeatmapStatus.Ranked,
			2 => BeatmapStatus.Approved,
			3 => BeatmapStatus.Qualified,
			4 => BeatmapStatus.Loved,
			_ => BeatmapStatus.LatestPending
		};
	}
	
	public static BeatmapStatus StatusFromOsuApi(
		this int status,
		bool frozen,
		BeatmapStatus prevStatus)
	{
		if (frozen) return prevStatus;
		
		return status switch
		{
			0 => BeatmapStatus.Ranked,
			2 => BeatmapStatus.LatestPending,
			3 => BeatmapStatus.Qualified,
			5 => BeatmapStatus.LatestPending,
			7 => BeatmapStatus.Ranked,
			8 => BeatmapStatus.Loved,
			_ => BeatmapStatus.LatestPending
		};
	}
}