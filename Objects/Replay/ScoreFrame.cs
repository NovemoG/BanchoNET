namespace BanchoNET.Objects.Replay;

public class ScoreFrame
{
	public int Time { get; set; }
	public byte Id { get; set; }
	public short Count300 { get; set; }
	public short Count100 { get; set; }
	public short Count50 { get; set; }
	public short Gekis { get; set; }
	public short Katus { get; set; }
	public short Misses { get; set; }
	public int TotalScore { get; set; }
	public short MaxCombo { get; set; }
	public short CurrentCombo { get; set; }
	public bool Perfect { get; set; }
	public byte CurrentHp { get; set; }
	public byte TagByte { get; set; }
	public bool ScoreV2 { get; set; }
	public double ComboPortion { get; set; }
	public double BonusPortion { get; set; }
}