using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;

[Serializable]
[MessagePackObject]
public readonly record struct RankedPlayCardHandReplayFrame
{
    [Key(0)]
    public required double Delay { get; init; }
    
    [Key(1)]
    public required Dictionary<Guid, RankedPlayCardState> Cards { get; init; }
    
    public RankedPlayCardHandReplayFrame RelativeTo(RankedPlayCardHandReplayFrame other) => this with
    {
        Cards = Cards.Where(entry => !other.Cards.Contains(entry)).ToDictionary(),
    };
}