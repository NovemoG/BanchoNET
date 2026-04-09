using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.HubClients.Metadata;
using BanchoNET.Core.Models.Players;
using Novelog.Abstractions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class MetadataHub(ILogger logger) : BaseHub<IMetadataClient>(logger)
{
    public async Task<BeatmapUpdates> GetChangesSince(
        int queueId
    ) {
        Logger.LogDebug("Invoked");
        return new BeatmapUpdates([], -1);
    }

    public async Task UpdateActivity(
        UserActivity? activity
    ) {
        Logger.LogDebug("Invoked");
    }

    public async Task UpdateStatus(
        UserStatus? status
    ) {
        Logger.LogDebug("Invoked");
    }

    public async Task BeginWatchingUserPresence() {
        Logger.LogDebug("Invoked");
    }

    public async Task EndWatchingUserPresence() {
        Logger.LogDebug("Invoked");
    }

    public async Task<MultiplayerPlaylistItemStats[]> BeginWatchingMultiplayerRoom(
        long id
    ) {
        Logger.LogDebug("Invoked");
        return [];
    }

    public async Task EndWatchingMultiplayerRoom(
        long id
    ) {
        Logger.LogDebug("Invoked");
    }

    public async Task RefreshFriends() {
        Logger.LogDebug("Invoked");
    }
}