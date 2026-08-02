using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Handlers.Lazer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace BanchoNET.Handlers.Lazer.Services;

public sealed class HubShutdownNotifier(
    ILogger logger,
    IHostApplicationLifetime lifetime,
    IHubContext<MetadataHub, IMetadataClient> metadata,
    IHubContext<SpectatorHub, ISpectatorClient> spectator,
    IHubContext<MultiplayerHub, IMultiplayerClient> multiplayer
) : IHostedService
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(2);

    public Task StartAsync(
        CancellationToken cancellationToken
    ) {
        lifetime.ApplicationStopping.Register(Notify);

        return Task.CompletedTask;
    }

    public Task StopAsync(
        CancellationToken cancellationToken
    ) => Task.CompletedTask;

    private void Notify()
    {
        try
        {
            Task.WhenAll(
                metadata.Clients.All.ServerShuttingDown(),
                spectator.Clients.All.ServerShuttingDown(),
                multiplayer.Clients.All.ServerShuttingDown()
            ).Wait(GracePeriod);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Failed to notify clients of shutdown: {ex.Message}", caller: nameof(HubShutdownNotifier));
        }
    }
}