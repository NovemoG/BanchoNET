using BanchoNET.Core.Abstractions;

namespace BanchoNET.Core.Models.Channels;

public interface IChannel : IHasOnlineId<string>,
    IEquatable<IChannel>
{
    string Name { get; }
    
    bool ReadOnly { get; }
    bool Instance { get; }
    
    bool IEquatable<IChannel>.Equals(IChannel? other)
    {
        if (other == null)
            return false;
        
        return OnlineId == other.OnlineId && Name == other.Name;
    }
}