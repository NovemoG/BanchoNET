namespace BanchoNET.Objects.Players;

public class ModeStats
{
	public long TotalScore { get; set; }
	public long RankedScore { get; set; }
	public ushort PP { get; set; }
	public float Accuracy { get; set; }
	public int PlayCount { get; set; }
	public int PlayTime { get; set; }
	public int MaxCombo { get; set; }
	public int Rank { get; set; }
	public int ReplayViews { get; set; }
	public Dictionary<Grade, int> Grades { get; set; } = [];
	
	public int TotalGekis { get; set; }
	public int TotalKatus { get; set; }
	public int Total300s { get; set; }
	public int Total100s { get; set; }
	public int Total50s { get; set; }
	public int TotalStdHits => Total300s + Total100s + Total50s;
	public int TotalTaikoHits => TotalStdHits + TotalGekis + TotalKatus;
	public int TotalManiaHits => TotalTaikoHits;
}