using System.Security.Cryptography;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Beatmaps;

namespace BanchoNET.Utils.Extensions;

public static class BeatmapExtensions
{
	public static BeatmapStatus ToBeatmapStatus(this string status)
	{
		return status switch
		{
			"love" => BeatmapStatus.Loved,
			"qualify" => BeatmapStatus.Qualified,
			"approve" => BeatmapStatus.Approved,
			"rank" => BeatmapStatus.Ranked,
			_ => BeatmapStatus.LatestPending
		};
	}
	
	public static string FullName(this Beatmap beatmap)
	{
		return $"{beatmap.Artist} - {beatmap.Title} [{beatmap.Name}]";
	}

	public static string Url(this Beatmap beatmap)
	{
		return $"https://osu.{AppSettings.Domain}/b/{beatmap.MapId}";
	}

	public static string Url(this BeatmapSet set)
	{
		return $"https://osu.{AppSettings.Domain}/s/{set.Id}";
	}

	public static string Embed(this Beatmap beatmap)
	{
		return $"[{beatmap.Url()} {beatmap.FullName()}]";
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

	public static BeatmapStatus StatusFromApi(
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

	public static int ToApiFromDirect(this int status)
	{
		return status switch
		{
			0 => 1,
			2 => 0,
			3 => 3,
			5 => 0,
			7 => 1, //TODO played before
			8 => 4,
			_ => 4
		};
	}

	public static BeatmapDto ToDto(this Beatmap beatmap)
	{
		return new BeatmapDto
		{
			MapId = beatmap.MapId,
			SetId = beatmap.SetId,
			Private = beatmap.Private,
			Mode = (byte)beatmap.Mode,
			Status = (sbyte)beatmap.Status,
			IsRankedOfficially = beatmap.IsRankedOfficially,
			MD5 = beatmap.MD5,
			Artist = beatmap.Artist,
			Title = beatmap.Title,
			Name = beatmap.Name,
			Creator = beatmap.Creator,
			SubmitDate = beatmap.SubmitDate,
			LastUpdate = beatmap.LastUpdate,
			TotalLength = beatmap.TotalLength,
			MaxCombo = beatmap.MaxCombo,
			Plays = beatmap.Plays,
			Passes = beatmap.Passes,
			Bpm = beatmap.Bpm,
			Cs = beatmap.Cs,
			Ar = beatmap.Ar,
			Od = beatmap.Od,
			Hp = beatmap.Hp,
			StarRating = beatmap.StarRating,
			NotesCount = beatmap.NotesCount,
			SlidersCount = beatmap.SlidersCount,
			SpinnersCount = beatmap.SpinnersCount
		};
	}

	public static BeatmapDto UpdateWith(this BeatmapDto currentBeatmap, Beatmap newBeatmap)
	{
		currentBeatmap.Status = newBeatmap.IsRankedOfficially || newBeatmap.Status == BeatmapStatus.Qualified
			? (sbyte)newBeatmap.Status
			: currentBeatmap.Status;
		currentBeatmap.IsRankedOfficially = newBeatmap.IsRankedOfficially;
		currentBeatmap.MD5 = newBeatmap.MD5;
		currentBeatmap.Artist = newBeatmap.Artist;
		currentBeatmap.Title = newBeatmap.Title;
		currentBeatmap.Name = newBeatmap.Name;
		currentBeatmap.Creator = newBeatmap.Creator;
		currentBeatmap.SubmitDate = newBeatmap.SubmitDate;
		currentBeatmap.LastUpdate = newBeatmap.LastUpdate;
		currentBeatmap.TotalLength = newBeatmap.TotalLength;
		currentBeatmap.MaxCombo = newBeatmap.MaxCombo;
		currentBeatmap.Bpm = newBeatmap.Bpm;
		currentBeatmap.Cs = newBeatmap.Cs;
		currentBeatmap.Ar = newBeatmap.Ar;
		currentBeatmap.Od = newBeatmap.Od;
		currentBeatmap.Hp = newBeatmap.Hp;
		currentBeatmap.StarRating = newBeatmap.StarRating;
		currentBeatmap.NotesCount = newBeatmap.NotesCount;
		currentBeatmap.SlidersCount = newBeatmap.SlidersCount;
		currentBeatmap.SpinnersCount = newBeatmap.SpinnersCount;
		
		return currentBeatmap;
	}
}