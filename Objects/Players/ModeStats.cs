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
	public Dictionary<Grade, int> Grades { get; set; } = [];
}