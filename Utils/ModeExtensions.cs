using BanchoNET.Objects;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Scores;

namespace BanchoNET.Utils;

public static class ModeExtensions
{
	public static ModeStats Copy(this ModeStats stats)
	{
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
			Total50s = stats.Total50s
		};
	}

	public static void UpdateHits(this ModeStats stats, Score score)
	{
		stats.Total300s += score.Count300;
		stats.Total100s += score.Count100;
		stats.Total50s += score.Count50;

		if (score.Mode.AsVanilla() is not (GameMode.VanillaMania or GameMode.VanillaTaiko)) return;
		
		stats.TotalGekis += score.Gekis;
		stats.TotalKatus += score.Katus;
	}
}