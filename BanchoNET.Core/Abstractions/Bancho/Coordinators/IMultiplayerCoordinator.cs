using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.Bancho.Coordinators;

public interface IMultiplayerCoordinator : ICoordinator
{
    Task CreateMatchAsync(MultiplayerMatch matchData, Player player);
    bool JoinPlayer(ushort id, string password, Player player);
    bool LeavePlayer(Player player);
    
    void JoinLobby(Player player);
    void LeavePlayerToLobby(Player player);
    void InviteToLobby(Player player, Player? target);
    
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