using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Lazer.Metadata;
using BanchoNET.Handlers.Lazer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed class BeatmapChangeNotifier(
    IHubContext<MetadataHub, IMetadataClient> metadata
) : IBeatmapChangeNotifier
{
    public Task BeatmapsetsChanged(
        int[] beatmapsetIds,
        int lastQueueId
    ) {
        return metadata.Clients.All.BeatmapSetsUpdated(new BeatmapUpdates(beatmapsetIds, lastQueueId));
    }
}