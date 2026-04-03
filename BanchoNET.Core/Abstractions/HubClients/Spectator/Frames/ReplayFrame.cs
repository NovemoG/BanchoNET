using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;

[MessagePackObject]
public class ReplayFrame
{
    [Key(0)]
    public double Time;

    [Key(1)]
    public FrameHeader? Header;
    
    public ReplayFrame() { }

    public ReplayFrame(
        double time
    ) {
        Time = time;
    }
}