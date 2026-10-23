using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Utils.Extensions;

public static class ModeExtensions
{
	public static readonly byte[] TrackedModes = [0, 1, 2, 3];

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