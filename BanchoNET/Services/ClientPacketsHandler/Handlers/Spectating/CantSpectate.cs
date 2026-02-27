using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task CantSpectate(User player, BinaryReader br)
    {
        if (player.Spectating == null)
        {
            Console.WriteLine($"{player.Username} sent cant spectate while not spectating");
            
            return Task.CompletedTask;
        }
        
        var bytes = new ServerPackets()
            .SpectatorCantSpectate(player.Id)
            .FinalizeAndGetContent();
        
        var host = player.Spectating;
        
        host.Enqueue(bytes);
        foreach (var spectator in host.Spectators)
            spectator.Enqueue(bytes);

        return Task.CompletedTask;
    }
}