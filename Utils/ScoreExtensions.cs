using AkatsukiPp;
using BanchoNET.Objects;
using BanchoNET.Services;

namespace BanchoNET.Utils;

public static class ScoreExtensions
{
	public static string ComputeOnlineChecksum(this Score score,
		string osuVersion,
		string clientHash,
		string storyboardChecksum)
	{
		if (score.Player == null || score.Beatmap == null) return "";

		return string.Format(
			"chickenmcnuggets{0}o15{1}{2}smustard{3}{4}uu{5}{6}{7}{8}{9}{10}{11}Q{12}{13}{15}{14:%y%m%d%H%M%S}{16}{17}",
			score.Count300 + score.Count100,
			score.Count50,
			score.Gekis,
			score.Katus,
			score.Misses,
			score.BeatmapMD5,
			score.MaxCombo,
			score.Perfect,
			score.Player.Username,
			score.TotalScore,
			score.Grade.ToString(),
			(int)score.Mods,
			score.Passed,
			score.Mode.AsVanilla(),
			score.ClientTime,
			osuVersion,
			clientHash,
			storyboardChecksum).CreateMD5();
	}
	
	public static void CalculateAccuracy(this Score score)
	{
		int objects;
        
		switch (score.Mode.AsVanilla())
		{
			case GameMode.VanillaStd:
				objects = score.Count300 + score.Count100 + score.Count50 + score.Misses;

				if (objects == 0)
				{
					score.Acc = 0f;
					return;
				}
				
				score.Acc = 100f * (score.Count300 * 300f + score.Count100 * 100f + score.Count50 * 50f) / (objects * 300f);
				break;
			case GameMode.VanillaTaiko:
				objects = score.Count300 + score.Count100 + score.Misses;

				if (objects == 0)
				{
					score.Acc = 0f;
					return;
				}

				score.Acc = 100f * (score.Count100 * 0.5f + score.Count300) / objects;
				break;
			case GameMode.VanillaCatch:
				objects = score.Count300 + score.Count100 + score.Count50 + score.Katus + score.Misses;

				if (objects == 0)
				{
					score.Acc = 0f;
					return;
				}

				score.Acc = 100f * (score.Count300 + score.Count100 + score.Count50) / objects;
				break;
			case GameMode.VanillaMania:
				objects = score.Count300 + score.Count100 + score.Count50 + score.Gekis + score.Katus + score.Misses;

				if (objects == 0)
				{
					score.Acc = 0f;
					return;
				}

				if ((score.Mods & Mods.ScoreV2) == Mods.ScoreV2)
				{
					score.Acc = 100f *
						(score.Gekis * 305f + score.Count300 * 300f + score.Katus * 200f + score.Count100 * 100f + score.Count50 * 50f)
						/ (objects * 305f);
					return;
				}

				score.Acc = 100f * 
				        ((score.Count300 + score.Gekis) * 300f + score.Katus * 200f + score.Count100 * 100f + score.Count50 * 50f)
				        / (objects * 300f);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(score), $"Invalid mode {score.Mode}");
		}
	}

	public static void CalculatePerformance(this Score score, string osuFilePath)
	{
		score.PP = MathF.Round((float)AkatsukiPpMethods.ComputePp(
			osuFilePath,
			(byte)score.Mode.AsVanilla(),
			(uint)score.Mods,
			new UIntPtr((uint)score.MaxCombo),
			score.Acc,
			new UIntPtr((uint)score.Count300),
			new UIntPtr((uint)score.Gekis),
			new UIntPtr((uint)score.Count100),
			new UIntPtr((uint)score.Katus),
			new UIntPtr((uint)score.Count50),
			new UIntPtr((uint)score.Misses)
		), 3);
		
		Console.WriteLine($"[Score Extensions] Submitted score pp: {score.PP}");
	}
	
	public static void ComputeSubmissionStatus(this Score score, Score? currentBest, bool submitByPP = true)
	{
		if (currentBest == null)
		{
			score.Status = SubmissionStatus.Best;
			return;
		}

		score.PreviousBest = currentBest;
		score.Status = SubmissionStatus.Submitted;

		if (!(score.PP > currentBest.PP)) return;
		
		score.Status = SubmissionStatus.Best;
		currentBest.Status = SubmissionStatus.Submitted;
	}

	public static void ComputeLeaderboardPosition(this Score score, BanchoHandler bancho)
	{
		
	}
}