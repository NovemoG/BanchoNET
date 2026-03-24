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
        return new BeatmapUpdates([], -1);
    }

    public async Task UpdateActivity(
        UserActivity? activity
    ) {

    }

    public async Task UpdateStatus(
        UserStatus? status
    ) {

    }

    public async Task BeginWatchingUserPresence() {
        
    }

    public async Task EndWatchingUserPresence() {
        
    }

    public async Task<MultiplayerPlaylistItemStats[]> BeginWatchingMultiplayerRoom(
        long id
    ) {
        return [];
    }

    public async Task EndWatchingMultiplayerRoom(
        long id
    ) {
        
    }

    public async Task RefreshFriends() {
        
    }
}