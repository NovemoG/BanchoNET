using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;

[Serializable]
[MessagePackObject]
public class RankedPlayDamageInfo : IEquatable<RankedPlayDamageInfo>
{
    [Key(0)]
    public required int Damage { get; init; }
    
    [Key(1)]
    public required int RawDamage { get; init; }
    
    [Key(2)]
    public required int OldLife { get; init; }
    
    [Key(3)]
    public required int NewLife { get; init; }

    public bool Equals(RankedPlayDamageInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Damage == other.Damage && RawDamage == other.RawDamage && OldLife == other.OldLife && NewLife == other.NewLife;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Equals((RankedPlayDamageInfo)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Damage, RawDamage, OldLife, NewLife);
}