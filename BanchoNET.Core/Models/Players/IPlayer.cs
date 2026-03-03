using BanchoNET.Core.Abstractions;

namespace BanchoNET.Core.Models.Players;

public interface IPlayer : IHasOnlineId<int>,
    IEquatable<IPlayer>
{
    string Username { get; }
    
    CountryCode CountryCode { get; }
    
    bool IsBot { get; }
    
    bool IEquatable<IPlayer>.Equals(IPlayer? other)
    {
        if (other == null)
            return false;

        return OnlineId == other.OnlineId && Username == other.Username;
    }
}