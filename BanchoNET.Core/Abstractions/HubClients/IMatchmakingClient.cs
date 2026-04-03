using BanchoNET.Core.Abstractions.HubClients.Multiplayer;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

namespace BanchoNET.Core.Abstractions.HubClients;

public interface IMatchmakingClient
{
    Task MatchmakingQueueJoined();
    
    Task MatchmakingQueueLeft();

    Task MatchmakingRoomInvitedWithParams(
        MatchmakingRoomInvitationParams invitation
    );

    Task MatchmakingRoomReady(
        long roomId,
        string password
    );

    Task MatchmakingLobbyStatusChanged(
        MatchmakingLobbyStatus status
    );

    Task MatchmakingQueueStatusChanged(
        MatchmakingQueueStatus status
    );

    Task MatchmakingItemSelected(
        int userId,
        long playlistItemId
    );

    Task MatchmakingItemDeselected(
        int userId,
        long playlistItemId
    );
}