namespace BanchoNET.Core.Models.Replay;

public class ScoreFrame
{
	public int Time { get; set; }
	public byte Id { get; set; }
	public ushort Count300 { get; set; }
	public ushort Count100 { get; set; }
	public ushort Count50 { get; set; }
	public ushort Gekis { get; set; }
	public ushort Katus { get; set; }
	public ushort Misses { get; set; }
	public int TotalScore { get; set; }
	public ushort MaxCombo { get; set; }
	public ushort CurrentCombo { get; set; }
	public bool Perfect { get; set; }
	public byte CurrentHp { get; set; }
	public byte TagByte { get; set; }
	public bool ScoreV2 { get; set; }
	public double ComboPortion { get; set; }
	public double BonusPortion { get; set; }
}