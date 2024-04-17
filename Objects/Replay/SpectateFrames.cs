namespace BanchoNET.Objects.Replay;

public class SpectateFrames
{
	public int Extra { get; set; }
	public required List<ReplayFrame> ReplayFrames { get; set; }
	public ReplayAction Action { get; set; }
	public required ScoreFrame ScoreFrame { get; set; }
	public int Sequence { get; set; }
}