using BanchoNET.Models.Dtos;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Players;

namespace BanchoNET.Objects;

public class Score
{
	public long? Id { get; set; }
	public string? BeatmapMD5 { get; set; }
	public Beatmap? Beatmap { get; set; }
	public int PlayerId { get; set; }
	public Player? Player { get; set; }

	public float PP	{ get; set; }
	public float Acc { get; set; }
	public int Count300 { get; set; }
	public int Count100 { get; set; }
	public int Count50 { get; set; }
	public int Gekis { get; set; }
	public int Katus { get; set; }
	public int Misses { get; set; }
	public int TotalScore { get; set; }
	public int MaxCombo { get; set; }
	public bool Perfect { get; set; }
	public bool Passed { get; set; }
	
	public SubmissionStatus Status { get; set; }
	public Grade Grade { get; set; }
	public Mods Mods { get; set; }
	public GameMode Mode { get; set; }
	public string ClientChecksum { get; set; }
	public ClientFlags ClientFlags { get; set; }
	public DateTime ClientTime { get; set; }
	public DateTime ServerTime { get; set; }
	public int TimeElapsed { get; set; }
	
	public int LeaderboardPosition { get; set; }
	public Score? PreviousBest { get; set; }

	public Score() { }
	
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
		Gekis = scoreDto.Gekis;
		Katus = scoreDto.Katus;
		Misses = scoreDto.Misses;
		TotalScore = scoreDto.Score;
		MaxCombo = scoreDto.MaxCombo;
		Perfect = scoreDto.Perfect;
		Status = (SubmissionStatus)scoreDto.Status;
		Passed = Status != SubmissionStatus.Failed;
		Grade = (Grade)scoreDto.Grade;
		Mods = (Mods)scoreDto.Mods;
		Mode = (GameMode)scoreDto.Mode;
		ClientChecksum = scoreDto.OnlineChecksum;
		ClientFlags = (ClientFlags)scoreDto.ClientFlags;
		TimeElapsed = scoreDto.TimeElapsed;
		ServerTime = scoreDto.PlayTime;
	}
}