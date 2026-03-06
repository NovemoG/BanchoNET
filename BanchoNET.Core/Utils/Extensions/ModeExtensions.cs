using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Utils.Extensions;

public static class ModeExtensions
{
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
				ReplayViews = stats.ReplayViews,
				Grades = new Dictionary<Grade, int>(stats.Grades),
				TotalGekis = stats.TotalGekis,
				TotalKatus = stats.TotalKatus,
				Total300s = stats.Total300s,
				Total100s = stats.Total100s,
				Total50s = stats.Total50s,
				TotalMisses = stats.TotalMisses
			};
		}

		public void UpdateHits(
			Score score
		) {
			stats.Total300s += score.Count300;
			stats.Total100s += score.Count100;
			stats.Total50s += score.Count50;
			stats.TotalMisses += score.Misses;
		
			if (score.Mode.AsVanilla() is not (GameMode.VanillaMania or GameMode.VanillaTaiko)) return;
		
			stats.TotalGekis += score.Gekis;
			stats.TotalKatus += score.Katus;
		}
	}
}