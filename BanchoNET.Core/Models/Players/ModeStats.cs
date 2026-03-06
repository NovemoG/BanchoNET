using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Models.Players;

public class ModeStats
{
	public long TotalScore { get; set; }
	public long RankedScore { get; set; }
	public int PP { get; set; }
	public float Accuracy { get; set; }
	public int PlayCount { get; set; }
	public int PlayTime { get; set; }
	public int MaxCombo { get; set; }
	public int Rank { get; set; }
	public int PeakRank { get; set; }
	public int ReplayViews { get; set; }
	public Dictionary<Grade, int> Grades { get; set; } = [];
	
	public long TotalGekis { get; set; }
	public long TotalKatus { get; set; }
	public long Total300s { get; set; }
	public long Total100s { get; set; }
	public long Total50s { get; set; }
	public long TotalMisses { get; set; }
	
	public long TotalStdHits => Total300s + Total100s + Total50s;
	public long TotalTaikoHits => TotalStdHits + TotalGekis + TotalKatus;
	public long TotalManiaHits => TotalTaikoHits;
	public long TotalCatchHits => TotalStdHits;
}