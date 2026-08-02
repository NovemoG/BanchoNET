using BanchoNET.Core.Models.Lazer.Metadata;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Abstractions.HubClients;

public interface IMetadataClient : IStatefulUserHubClient
{
    Task BeatmapSetsUpdated(
        BeatmapUpdates updates
    );

    Task UserPresenceUpdated(
        int userId,
        UserPresence? status
    );

    Task FriendPresenceUpdated(
        int userId,
        UserPresence? presence
    );

    Task DailyChallengeUpdated(
        DailyChallengeInfo? info
    );

    Task MultiplayerRoomScoreSet(
        MultiplayerRoomScoreSetEvent roomScoreSetEvent
    );
}