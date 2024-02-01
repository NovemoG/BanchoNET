namespace BanchoNET.Objects.Replay;

public class SpectateFrames
{
	public int Extra { get; set; }
	public List<ReplayFrame> ReplayFrames { get; set; }
	public byte Action { get; set; }
	public ScoreFrame ScoreFrame { get; set; }
}