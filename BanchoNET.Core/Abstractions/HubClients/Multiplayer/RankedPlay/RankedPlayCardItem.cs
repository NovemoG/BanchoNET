using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;

[Serializable]
[MessagePackObject]
public class RankedPlayCardItem : IEquatable<RankedPlayCardItem>
{
    [Key(0)]
    public Guid ID { get; set; } = Guid.NewGuid();
    
    public bool Equals(RankedPlayCardItem? other)
        => other != null && ID.Equals(other.ID);

    public override bool Equals(object? obj)
        => obj is RankedPlayCardItem other && Equals(other);
    
    public override int GetHashCode() => ID.GetHashCode();
}