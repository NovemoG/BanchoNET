using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Stable.Multiplayer;

namespace BanchoNET.Core.Abstractions.Bancho.Coordinators;

public interface IMultiplayerCoordinator : ICoordinator
{
    /// <summary>
    /// Opens a lobby, or refuses to when its history cannot be recorded.
    /// </summary>
    Task<bool> CreateMatchAsync(MultiplayerMatch matchData, Player player, long? channelId = null);
    Task<bool> JoinPlayer(ushort id, string password, Player player);
    
    Task<bool> LeavePlayer(Player player, MultiplayerEventType reason = MultiplayerEventType.PlayerLeft);

    void JoinLobby(Player player);
    Task LeavePlayerToLobby(Player player, MultiplayerEventType reason = MultiplayerEventType.PlayerLeft);
    void InviteToLobby(Player player, Player? target);

    /// <summary>
    /// Puts everyone into play and opens a game for the map, which incoming scores attach to.
    /// </summary>
    Task StartMatch(MultiplayerMatch match);
    Task AbortMatch(MultiplayerMatch match);
    void AllPlayersLoaded(MultiplayerMatch match);

    /// <summary>
    /// Closes the current game and frees the lobby for the next map.
    /// </summary>
    /// <param name="forced">
    /// The completion deadline elapsed while clients were still playing. Only the lobby is
    /// unblocked - the scoreboard stays open.
    /// </param>
    Task CompleteGame(
        MultiplayerMatch match,
        bool forced = false
    );

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