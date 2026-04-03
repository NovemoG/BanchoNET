using BanchoNET.Core.Abstractions.HubClients.Multiplayer;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.MultiplayerRooms;
using BanchoNET.Core.Models.Api;

namespace BanchoNET.Core.Abstractions.HubClients;

public interface IMultiplayerClient : IMatchmakingClient, IRankedPlayClient
{
    Task RoomStateChanged(
        MultiplayerRoomState state
    );

    Task UserJoined(
        MultiplayerRoomUser user
    );

    Task UserLeft(
        MultiplayerRoomUser user
    );

    Task UserKicked(
        MultiplayerRoomUser user
    );

    Task Invited(
        int invitedBy,
        long roomID,
        string password
    );

    Task HostChanged(
        int userId
    );

    Task SettingsChanged(
        MultiplayerRoomSettings newSettings
    );

    Task UserStateChanged(
        int userId,
        MultiplayerUserState state
    );

    Task MatchUserStateChanged(
        int userId,
        MatchUserState state
    );

    Task MatchRoomStateChanged(
        MatchRoomState state
    );

    Task MatchEvent(
        MatchServerEvent e
    );

    Task UserBeatmapAvailabilityChanged(
        int userId,
        BeatmapAvailability beatmapAvailability
    );

    Task UserStyleChanged(
        int userId,
        int? beatmapId,
        int? rulesetId
    );

    Task UserModsChanged(
        int userId,
        IEnumerable<ApiMod> mods
    );

    Task LoadRequested();

    Task GameplayStarted();

    Task GameplayAborted(
        GameplayAbortReason reason
    );
    
    Task ResultsReady();

    Task PlaylistItemAdded(
        MultiplayerPlaylistItem item
    );

    Task PlaylistItemRemoved(
        long playlistItemId
    );

    Task PlaylistItemChanged(
        MultiplayerPlaylistItem item
    );

    Task UserVotedToSkipIntro(
        int userId,
        bool voted
    );
    
    Task VoteToSkipIntroPassed();
}