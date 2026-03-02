using System.Security.Cryptography;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Utils.Extensions;

public static class BeatmapExtensions
{
	private static readonly TimeSpan[] ApiCheckIntervals = [
		TimeSpan.FromDays(3),
		TimeSpan.FromDays(5),
		TimeSpan.FromDays(7)
	];
	
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
	
	public static string Url(this BeatmapSet set)
	{
		return $"https://osu.{AppSettings.Domain}/s/{set.Id}";
	}
	
	extension(
		Beatmap beatmap
	) {
		public string Url() {
			return $"https://osu.{AppSettings.Domain}/b/{beatmap.Id}";
		}

		public string FullName() {
			return $"{beatmap.Artist} - {beatmap.Title} [{beatmap.Name}]";
		}

		public string Embed() {
			return $"[{beatmap.Url()} {beatmap.FullName()}]";
		}

		public bool HasLeaderboard() {
			return beatmap.Status is
				BeatmapStatus.Approved or
				BeatmapStatus.Ranked or
				BeatmapStatus.Loved or
				BeatmapStatus.Qualified;
		}

		public bool AwardsPP()
		{
			return beatmap.Status is BeatmapStatus.Approved or BeatmapStatus.Ranked;
		}

		public bool ShouldRecheckApi() {
			if (beatmap.IsRankedOfficially) return false;
			if (beatmap.Status < BeatmapStatus.LatestPending) return false;
			if (beatmap.NextApiCheck < DateTime.UtcNow)
			{
				beatmap.UpdateApiChecks();
				return true;
			}

			return false;
		}

		public void UpdateApiChecks() {
			beatmap.NextApiCheck = DateTime.UtcNow.Add(ApiCheckIntervals[beatmap.ApiChecks]);
			if (beatmap.ApiChecks < ApiCheckIntervals.Length - 1) beatmap.ApiChecks++;
		}
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
			MapId = beatmap.Id,
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
		currentBeatmap.Status = newBeatmap.IsRankedOfficially //TODO preservestatusonranked .env
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