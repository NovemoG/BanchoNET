using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;

[MessagePackObject]
public class LegacyReplayFrame(double time, float? mouseX, float? mouseY, ReplayButtonState buttonState)
    : ReplayFrame(time)
{
    [Key(1)]
    public float? MouseX = mouseX;

    [Key(2)]
    public float? MouseY = mouseY;

    [Key(3)]
    public ReplayButtonState ButtonState = buttonState;
}