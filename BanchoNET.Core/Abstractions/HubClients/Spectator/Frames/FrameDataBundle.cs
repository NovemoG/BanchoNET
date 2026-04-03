using System.Text.Json.Serialization;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Spectator.Frames;

[Serializable]
[MessagePackObject]
[method: JsonConstructor]
public class FrameDataBundle(
    FrameHeader header,
    IList<LegacyReplayFrame> frames
) {
    [Key(0)]
    public FrameHeader Header { get; set; } = header;

    [Key(1)]
    public IList<LegacyReplayFrame> Frames { get; set; } = frames;
}