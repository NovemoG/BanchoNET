using BanchoNET.Core.Abstractions;

namespace BanchoNET.Core.Models.Players;

public interface IUser : IHasOnlineId<int>,
    IEquatable<IUser>
{
    string Username { get; }
    
    CountryCode CountryCode { get; }
    
    bool IsBot { get; }
    
    bool IEquatable<IUser>.Equals(IUser? other)
    {
        if (other == null)
            return false;

        return OnlineId == other.OnlineId && Username == other.Username;
    }
}