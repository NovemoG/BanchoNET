using BanchoNET.Objects.Players;
using BanchoNET.Packets;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
    private Task CantSpectate(Player player, BinaryReader br)
    {
        if (player.Spectating == null)
        {
            Console.WriteLine($"{player.Username} sent cant spectate while not spectating");
            
            return Task.CompletedTask;
        }
        
        using var cantSpectatePacket = new ServerPackets();
        cantSpectatePacket.SpectatorCantSpectate(player.Id);
        var bytes = cantSpectatePacket.GetContent();
        
        var host = player.Spectating;
        
        host.Enqueue(bytes);
        foreach (var spectator in host.Spectators)
            spectator.Enqueue(bytes);

        return Task.CompletedTask;
    }
}