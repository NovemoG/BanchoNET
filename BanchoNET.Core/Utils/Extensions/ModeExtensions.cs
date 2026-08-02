using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Utils.Extensions;

public static class ModeExtensions
{
	/// <summary>
	/// RelaxMania (7) and the autopilot variants beyond AutopilotStd (9-11) are not real modes
	/// </summary>
	public static readonly byte[] TrackedModes = [0, 1, 2, 3, 4, 5, 6, 8];

	extension(
		ModeStats stats
	) {
		public ModeStats Copy() {
			return new ModeStats
			{
				TotalScore = stats.TotalScore,
				RankedScore = stats.RankedScore,
				PP = stats.PP,
				Accuracy = stats.Accuracy,
				PlayCount = stats.PlayCount,
				PlayTime = stats.PlayTime,
				MaxCombo = stats.MaxCombo,
				Rank = stats.Rank,
				PeakRank = stats.PeakRank,
				PeakRankDate = stats.PeakRankDate,
				ReplayViews = stats.ReplayViews,
				Grades = new Dictionary<Grade, int>(stats.Grades),
				TotalGekis = stats.TotalGekis,
				TotalKatus = stats.TotalKatus,
				Total300s = stats.Total300s,
				Total100s = stats.Total100s,
				Total50s = stats.Total50s,
			};
		}
	}

	extension(
		StatsDto stats
	) {
		public ModeStats ToModeStats(
			int rank
		) {
			return new ModeStats
			{
				TotalScore = stats.TotalScore,
				RankedScore = stats.RankedScore,
				PP = stats.PP,
				Accuracy = stats.Accuracy,
				PlayCount = stats.PlayCount,
				PlayTime = stats.PlayTime,
				MaxCombo = stats.MaxCombo,
				ReplayViews = stats.ReplayViews,
				Rank = rank,
				PeakRank = stats.PeakRank,
				PeakRankDate = stats.PeakRankDate,
				Grades = {
					{ Grade.XH, stats.XHCount },
					{ Grade.X, stats.XCount },
					{ Grade.SH, stats.SHCount },
					{ Grade.S, stats.SCount },
					{ Grade.A, stats.ACount }
				},
				TotalGekis = stats.TotalGekis,
				TotalKatus = stats.TotalKatus,
				Total300s = stats.Total300s,
				Total100s = stats.Total100s,
				Total50s = stats.Total50s,
			};
		}

		public void UpdateHits(
			Score score
		) {
			stats.Total300s += score.Count300;
			stats.Total100s += score.Count100;
			stats.Total50s += score.Count50;
			
			if (score.Mode.AsVanilla() is not (GameMode.VanillaMania or GameMode.VanillaTaiko)) return;
			
			stats.TotalGekis += score.Gekis;
			stats.TotalKatus += score.Katus;
		}

		public void IncreaseGrade(
			Grade grade
		) {
			switch (grade)
			{
				case Grade.A: stats.ACount++; break;
				case Grade.S: stats.SCount++; break;
				case Grade.SH: stats.SHCount++; break;
				case Grade.X: stats.XCount++; break;
				case Grade.XH: stats.XHCount++; break;
			}
		}

		public void DecreaseGrade(
			Grade grade
		) {
			switch (grade)
			{
				case Grade.A: stats.ACount--; break;
				case Grade.S: stats.SCount--; break;
				case Grade.SH: stats.SHCount--; break;
				case Grade.X: stats.XCount--; break;
				case Grade.XH: stats.XHCount--; break;
			}
		}
	}

	public static string PlayingVerb(
		this int rulesetId
	) {
		return rulesetId switch
		{
			0 => "Clicking circles",
			1 => "Bashing drums",
			2 => "Catching fruits",
			3 => "Smashing keys",
			_ => "Playing"
		};
	}
}