using System.Security.Cryptography;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

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

	public static string ToApiBeatmapStatus(
		this BeatmapStatus status
	) {
		return status switch
		{
			BeatmapStatus.Graveyard => "graveyard",
			BeatmapStatus.WIP => "wip",
			BeatmapStatus.LatestPending => "pending",
			BeatmapStatus.Ranked => "ranked",
			BeatmapStatus.Approved => "approved",
			BeatmapStatus.Qualified => "qualified",
			BeatmapStatus.Loved => "loved",
			_ => "unknown"
		};
	}

	public static BeatmapStatus FromApiBeatmapStatus(
		this string status
	) {
		return status switch
		{
			"graveyard" => BeatmapStatus.Graveyard,
			"wip" => BeatmapStatus.WIP,
			"pending" => BeatmapStatus.LatestPending,
			"ranked" => BeatmapStatus.Ranked,
			"approved" => BeatmapStatus.Approved,
			"qualified" => BeatmapStatus.Qualified,
			"loved" => BeatmapStatus.Loved,
			_ => BeatmapStatus.Graveyard
		};
	}
	
	extension(
		Beatmapset set
	) {
		public string Url()
		{
			return $"https://osu.{AppSettings.Domain}/s/{set.Id}";
		}

		public bool ShouldRecheckApi() {
			if (set.IsRankedOfficially) return false;
			if (set.Status < BeatmapStatus.LatestPending) return false;
			if (set.NextApiCheck < DateTime.UtcNow)
			{
				set.UpdateApiChecks();
				return true;
			}

			return false;
		}

		public void UpdateApiChecks() {
			set.NextApiCheck = DateTime.UtcNow.Add(ApiCheckIntervals[set.ApiChecks]);
			if (set.ApiChecks < ApiCheckIntervals.Length - 1) set.ApiChecks++;
		}
	}

	public static bool HasNominations(
		this BeatmapStatus status
	) {
		return status is BeatmapStatus.Ranked or BeatmapStatus.Approved or BeatmapStatus.Qualified;
	}
	
	extension(
		Beatmap beatmap
	) {
		public string Url() {
			return $"https://osu.{AppSettings.Domain}/b/{beatmap.Id}";
		}

		public string FullName() {
			return $"{beatmap.Set.Artist} - {beatmap.Set.Title} [{beatmap.Version}]";
		}

		public string FileName() {
			return $"{beatmap.Set.Artist} - {beatmap.Set.Title} ({beatmap.Set.CreatorName}) [{beatmap.Version}].osu";
		}
		
		public string DisplayTitle() {
			return $"{beatmap.Set.ArtistUnicode} - {beatmap.Set.TitleUnicode} {beatmap.Set.CreatorName} {beatmap.Version}".Trim();
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

		public bool AwardsPp() {
			return beatmap.Status is BeatmapStatus.Approved or BeatmapStatus.Ranked;
		}

		public bool ShouldRecheckApi() {
			if (beatmap.Set.IsRankedOfficially) return false;
			if (beatmap.Status < BeatmapStatus.LatestPending) return false;
			if (beatmap.Set.NextApiCheck < DateTime.UtcNow)
			{
				beatmap.UpdateApiChecks();
				return true;
			}

			return false;
		}

		public void UpdateApiChecks() {
			var set = beatmap.Set;
			
			set.NextApiCheck = DateTime.UtcNow.Add(ApiCheckIntervals[set.ApiChecks]);
			if (set.ApiChecks < ApiCheckIntervals.Length - 1) set.ApiChecks++;
		}
	}

	public static bool CheckLocalBeatmapMD5(this string beatmapFile, string beatmapMD5)
	{
		using var md5 = MD5.Create();
		using var stream = File.OpenRead(beatmapFile);
		
		var hash = md5.ComputeHash(stream);
		var md5String = Convert.ToHexString(hash);

		return md5String.Equals(beatmapMD5, StringComparison.OrdinalIgnoreCase);
	}

	public static BeatmapStatus StatusFromApi(
		this int status,
		bool frozen,
		BeatmapStatus prevStatus
	) {
		if (frozen) return prevStatus;
		
		return status switch
		{
			-2 => BeatmapStatus.Graveyard,
			-1 => BeatmapStatus.WIP,
			0 => BeatmapStatus.LatestPending,
			1 => BeatmapStatus.Ranked,
			2 => BeatmapStatus.Approved,
			3 => BeatmapStatus.Qualified,
			4 => BeatmapStatus.Loved,
			_ => BeatmapStatus.Graveyard
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

	public static int ToLegacyStatus(
		this BeatmapStatus status
	) {
		return status switch
		{
			BeatmapStatus.Graveyard => 0,
			BeatmapStatus.WIP => 0,
			BeatmapStatus.LatestPending => 0,
			BeatmapStatus.Ranked => 2,
			BeatmapStatus.Approved => 3,
			BeatmapStatus.Qualified => 4,
			BeatmapStatus.Loved => 5,
			_ => -2
		};
	}

	public static BeatmapsetDto ToDto(
		this Beatmapset set
	) {
		return new BeatmapsetDto
		{
			Id = set.Id,
			Artist = set.Artist,
			ArtistUnicode = set.ArtistUnicode,
			Title = set.Title,
			TitleUnicode = set.TitleUnicode,
			IsRankedOfficially = set.IsRankedOfficially,
			IsPrivateUpload = set.IsPrivateUpload,
			Status = set.Status,
			FavoriteCount = set.FavoriteCount,
			PlayCount = set.PlayCount,
			Source = set.Source,
			GenreId = set.GenreId,
			LanguageId = set.LanguageId,
			Video = set.Video,
			Storyboard = set.Storyboard,
			Bpm = set.Bpm,
			IsScoreable = set.IsScoreable,
			Tags = set.Tags,
			Description = set.Description,
			SubmittedDate = set.SubmitDate,
			LastUpdated = set.LastUpdate,
			RankedDate = set.RankedDate,
			Ratings = set.Ratings,

			CreatorName = set.CreatorName,
			CreatorId = 1, //TODO for now it's bancho bot

			Beatmaps = set.Beatmaps.Select(b => b.ToDto()).ToList(),
		};
	}

	public static BeatmapDto ToDto(this Beatmap beatmap)
	{
		return new BeatmapDto
		{
			Id = beatmap.Id,
			SetId = beatmap.BeatmapsetId,
			MD5 = beatmap.Checksum,
			Version = beatmap.Version,
			Mode = beatmap.Mode,
			Status = beatmap.Status,
			StarRating = beatmap.StarRating,
			Bpm = beatmap.Bpm,
			Cs = beatmap.Cs,
			Ar = beatmap.Ar,
			Od = beatmap.Od,
			Hp = beatmap.Hp,
			CirclesCount = beatmap.CirclesCount,
			SlidersCount = beatmap.SlidersCount,
			SpinnersCount = beatmap.SpinnersCount,
			MaxCombo = beatmap.MaxCombo,
			TotalLength = beatmap.TotalLength,
			HitLength = beatmap.HitLength,
			IsScoreable = beatmap.IsScoreable,
			LastUpdated = beatmap.LastUpdated,
			Plays = beatmap.Plays,
			Passes = beatmap.Passes,
			Fails = beatmap.Fails,
			Exits = beatmap.Exits,
			Owners = [
				new BeatmapOwner
				{
					BeatmapId = beatmap.Id,
					PlayerId = 1,
					Username = "Bancho Bot"
				}
			]
		};
	}

	public static BeatmapDto UpdateWith(
		this BeatmapDto currentBeatmap,
		Beatmap newBeatmap,
		bool rankedOfficially
	) {
		currentBeatmap.Status = rankedOfficially //TODO preservestatusonranked .env
			? newBeatmap.Status
			: currentBeatmap.Status;
		
		currentBeatmap.Id = newBeatmap.Id;
		currentBeatmap.SetId = newBeatmap.BeatmapsetId;
		currentBeatmap.MD5 = newBeatmap.Checksum;
		currentBeatmap.Version = newBeatmap.Version;
		currentBeatmap.Mode = newBeatmap.Mode;
		currentBeatmap.StarRating = newBeatmap.StarRating;
		currentBeatmap.Bpm = newBeatmap.Bpm;
		currentBeatmap.Cs = newBeatmap.Cs;
		currentBeatmap.Ar = newBeatmap.Ar;
		currentBeatmap.Od = newBeatmap.Od;
		currentBeatmap.Hp = newBeatmap.Hp;
		currentBeatmap.CirclesCount = newBeatmap.CirclesCount;
		currentBeatmap.SlidersCount = newBeatmap.SlidersCount;
		currentBeatmap.SpinnersCount = newBeatmap.SpinnersCount;
		currentBeatmap.MaxCombo = newBeatmap.MaxCombo;
		currentBeatmap.TotalLength = newBeatmap.TotalLength;
		currentBeatmap.HitLength = newBeatmap.HitLength;
		currentBeatmap.LastUpdated = newBeatmap.LastUpdated;
		
		newBeatmap.Status = currentBeatmap.Status;
		newBeatmap.Plays = currentBeatmap.Plays;
		newBeatmap.Passes = currentBeatmap.Passes;
		newBeatmap.Fails = currentBeatmap.Fails;
		newBeatmap.Exits = currentBeatmap.Exits;
		newBeatmap.IsScoreable = currentBeatmap.IsScoreable;
		newBeatmap.MaxStatistics = currentBeatmap.MaximumStatistics;
		
		return currentBeatmap;
	}

	public static BeatmapsetDto UpdateWith(
		this BeatmapsetDto currentBeatmapset,
		Beatmapset newBeatmapset
	) {
		currentBeatmapset.Status = newBeatmapset.IsRankedOfficially //TODO preservestatusonranked .env
			? newBeatmapset.Status
			: currentBeatmapset.Status;
		
		currentBeatmapset.Id = newBeatmapset.Id;
		currentBeatmapset.Artist = newBeatmapset.Artist;
		currentBeatmapset.ArtistUnicode = newBeatmapset.ArtistUnicode;
		currentBeatmapset.Title = newBeatmapset.Title;
		currentBeatmapset.TitleUnicode = newBeatmapset.TitleUnicode;
		currentBeatmapset.IsRankedOfficially = newBeatmapset.IsRankedOfficially;
		currentBeatmapset.IsPrivateUpload = newBeatmapset.IsPrivateUpload;
		currentBeatmapset.Source = newBeatmapset.Source;
		currentBeatmapset.GenreId = newBeatmapset.GenreId;
		currentBeatmapset.LanguageId = newBeatmapset.LanguageId;
		currentBeatmapset.Video = newBeatmapset.Video;
		currentBeatmapset.Storyboard = newBeatmapset.Storyboard;
		currentBeatmapset.Bpm = newBeatmapset.Bpm;
		currentBeatmapset.Tags = newBeatmapset.Tags;
		currentBeatmapset.Description = newBeatmapset.Description;
		currentBeatmapset.SubmittedDate = newBeatmapset.SubmitDate;
		currentBeatmapset.LastUpdated = newBeatmapset.LastUpdate;
		currentBeatmapset.RankedDate = newBeatmapset.RankedDate;
		currentBeatmapset.Ratings = newBeatmapset.Ratings;
		currentBeatmapset.CreatorName = newBeatmapset.CreatorName;
		currentBeatmapset.CreatorId = newBeatmapset.CreatorId;

		newBeatmapset.Status = currentBeatmapset.Status;
		newBeatmapset.FavoriteCount = currentBeatmapset.FavoriteCount;
		newBeatmapset.PlayCount = currentBeatmapset.PlayCount;
		newBeatmapset.Ratings = currentBeatmapset.Ratings;
		newBeatmapset.Rating = (float)currentBeatmapset.Ratings.Average();
		newBeatmapset.IsScoreable = currentBeatmapset.IsScoreable;
		
		if (currentBeatmapset.Status is BeatmapStatus.Ranked or BeatmapStatus.Approved
		    && newBeatmapset.RankedDate == null)
		{
			newBeatmapset.RankedDate = newBeatmapset.LastUpdate;
			currentBeatmapset.RankedDate = newBeatmapset.LastUpdate;
		}
		
		return currentBeatmapset;
	}
}