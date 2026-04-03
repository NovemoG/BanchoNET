using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;

[Serializable]
[MessagePackObject]
public readonly record struct RankedPlayCardState
{
    [Key(0)]
    public required bool Hovered { get; init; }

    [Key(1)]
    public required bool Pressed { get; init; }

    [Key(2)]
    public required bool Selected { get; init; }

    [Key(3)]
    public required bool Dragged { get; init; }

    [Key(4)]
    public required int Order { get; init; }

    [Key(5)]
    public float DragX { get; init; }

    [Key(6)]
    public float DragY { get; init; }
}