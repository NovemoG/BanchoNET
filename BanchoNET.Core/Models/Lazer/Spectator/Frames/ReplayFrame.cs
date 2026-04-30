using MessagePack;

namespace BanchoNET.Core.Models.Lazer.Spectator.Frames;

[MessagePackObject]
public class ReplayFrame
{
    [Key(0)]
    public double Time;
    
    public ReplayFrame() { }

    public ReplayFrame(
        double time
    ) {
        Time = time;
    }
}