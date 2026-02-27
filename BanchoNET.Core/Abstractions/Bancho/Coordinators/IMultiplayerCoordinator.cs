using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Users;

namespace BanchoNET.Core.Abstractions.Bancho.Coordinators;

public interface IMultiplayerCoordinator : ICoordinator
{
    Task CreateMatchAsync(MultiplayerMatch matchData, User player);
    bool JoinPlayer(ushort id, string password, User player);
    bool LeavePlayer(User player);
    
    void JoinLobby(User player);
    void LeavePlayerToLobby(User player);
    void InviteToLobby(User player, User? target);
    
    void StartMatch(MultiplayerMatch match);
    void EndMatch(MultiplayerMatch match);
    
    void EnqueueTo(
        MultiplayerMatch match,
        byte[] data,
        List<int>? immune = null,
        bool toLobby = true
    );
    void EnqueueStateTo(
        MultiplayerMatch match,
        bool toLobby = true
    );
    void EnqueueDisposeFor(MultiplayerMatch match);
}