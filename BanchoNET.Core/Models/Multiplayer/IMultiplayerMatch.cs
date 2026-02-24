using BanchoNET.Core.Abstractions;

namespace BanchoNET.Core.Models.Multiplayer;

public interface IMultiplayerMatch : IHasOnlineId<int>,
    IEquatable<IMultiplayerMatch>
{
    ushort Id { get; init; }
    
    string Name { get; set; }
    bool InProgress { get; set; }
    MultiplayerSlot[] Slots { get; set; }
    
    bool IEquatable<IMultiplayerMatch>.Equals(IMultiplayerMatch? other)
    {
        if (other == null)
            return false;

        return OnlineId == other.OnlineId && Id == other.Id;
    }
}