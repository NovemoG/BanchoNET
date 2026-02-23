namespace BanchoNET.Core.Models.Replay;

public class ReplayFrame
{
	public byte ButtonState { get; set; }
	public byte TaikoByte { get; set; }
	public float MouseX { get; set; }
	public float MouseY { get; set; }
	public int Time { get; set; }
}