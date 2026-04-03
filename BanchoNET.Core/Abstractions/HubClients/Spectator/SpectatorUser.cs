using BanchoNET.Core.Models.Players;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Spectator;

[Serializable]
[MessagePackObject]
public class SpectatorPlayer : IPlayer, IEquatable<SpectatorPlayer>
{
    [Key(0)]
    public int OnlineId { get; set; }

    [Key(1)]
    public string Username { get; set; } = string.Empty;

    [IgnoreMember]
    public CountryCode CountryCode => CountryCode.Unknown;

    [IgnoreMember]
    public bool IsBot => false;

    public bool Equals(
        SpectatorPlayer? other
    ) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return OnlineId == other.OnlineId;
    }

    public override bool Equals(object? obj) => Equals(obj as SpectatorPlayer);

    public override int GetHashCode() => OnlineId;
}