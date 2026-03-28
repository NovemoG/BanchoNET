using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Scores;

public class Score
{
	public long Id { get; set; }
	public string? BeatmapMD5 { get; set; }
	public Beatmap? Beatmap { get; set; }
	public int PlayerId { get; set; }
	public Player? Player { get; set; }

	public float PP	{ get; set; }
	public float Acc { get; set; }
	public int Count300 { get; set; }
	public int Count100 { get; set; }
	public int Count50 { get; set; }
	public int Misses { get; set; }
	/// <summary>
	/// On lazer used as LargeTickHit
	/// </summary>
	public int Gekis { get; set; }
	/// <summary>
	/// On lazer used as SliderTailHit
	/// </summary>
	public int Katus { get; set; }
	public int IgnoreHit { get; set; }
	public int IgnoreMiss { get; set; }
	public int TotalScore { get; set; }
	public int MaxCombo { get; set; }
	public bool Perfect { get; set; }
	public bool Passed { get; set; }
	public bool Preserve { get; set; }
	public bool Ranked { get; set; }
	public bool Processed { get; set; }
	public bool HasReplay { get; set; }
	
	public SubmissionStatus Status { get; set; }
	public Grade Grade { get; set; }
	public LegacyMods Mods { get; set; }
	public GameMode Mode { get; set; }
	public string ClientChecksum { get; set; } = null!;
	public ClientFlags ClientFlags { get; set; }
	/// <summary>
	/// On lazer used as EndedAt
	/// </summary>
	public DateTimeOffset ClientTime { get; set; }
	public DateTimeOffset? StartTime { get; set; }
	public int TimeElapsed { get; set; }
	
	public int LeaderboardPosition { get; set; }
	public Score? PreviousBest { get; set; }
	
	public Score() { }

	public Score(
		IReadOnlyList<string> scoreData,
		Beatmap beatmap,
		Player player
	) {
		if (!Enum.TryParse(scoreData[10], out Grade grade))
			return;
		
		var mods = (LegacyMods)int.Parse(scoreData[11]);
		
		Passed = scoreData[12] == "True";
		Mode = ((GameMode)int.Parse(scoreData[13])).FromMods(mods);
		
		Beatmap = beatmap;
		BeatmapMD5 = beatmap.MD5;
		Player = player;
		PlayerId = player.Id;
		
		ClientChecksum = scoreData[0];
		Count300 = int.Parse(scoreData[1]);
		Count100 = int.Parse(scoreData[2]);
		Count50 = int.Parse(scoreData[3]);
		Gekis = int.Parse(scoreData[4]);
		Katus = int.Parse(scoreData[5]);
		Misses = int.Parse(scoreData[6]);
		TotalScore = int.Parse(scoreData[7]);
		MaxCombo = int.Parse(scoreData[8]);
		Perfect = scoreData[9] == "True";
		Grade = grade;
		Mods = mods;
		
		ClientTime = DateTime.SpecifyKind(
			DateTime.ParseExact(scoreData[14], "yyMMddHHmmss", null),
			DateTimeKind.Utc
		);
		
		ClientFlags = (ClientFlags)int.Parse(scoreData[15]);
		Ranked = beatmap.Status is BeatmapStatus.Ranked or BeatmapStatus.Approved;
		Processed = true; //always processed instantly on score submission
	}
	
	public Score(ScoreDto scoreDto)
	{
		Id = scoreDto.Id;
		BeatmapMD5 = scoreDto.BeatmapMD5;
		PlayerId = scoreDto.PlayerId;
		PP = scoreDto.PP;
		Acc = scoreDto.Acc;
		Count300 = scoreDto.Count300;
		Count100 = scoreDto.Count100;
		Count50 = scoreDto.Count50;
		Misses = scoreDto.Misses;
		Gekis = scoreDto.Gekis;
		Katus = scoreDto.Katus;
		IgnoreHit = scoreDto.IgnoreHit;
		IgnoreMiss = scoreDto.IgnoreMiss;
		TotalScore = scoreDto.LegacyTotalScore;
		MaxCombo = scoreDto.MaxCombo;
		Perfect = scoreDto.LegacyPerfect;
		Status = (SubmissionStatus)scoreDto.Status;
		Passed = Status != SubmissionStatus.Failed;
		Preserve = scoreDto.Preserve;
		Processed = scoreDto.Processed;
		Grade = (Grade)scoreDto.Grade;
		Mods = (LegacyMods)scoreDto.Mods;
		Mode = (GameMode)scoreDto.Mode;
		ClientChecksum = scoreDto.OnlineChecksum ?? string.Empty;
		ClientFlags = (ClientFlags)scoreDto.ClientFlags;
		TimeElapsed = scoreDto.TimeElapsed;
		ClientTime = scoreDto.PlayTime;
		StartTime = scoreDto.StartTime;
		HasReplay = scoreDto.HasReplay;
	}
}