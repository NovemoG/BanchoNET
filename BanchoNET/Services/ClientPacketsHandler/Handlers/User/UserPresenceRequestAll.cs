using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task UserPresenceRequestAll(Player player, BinaryReader br)
    {
        // Only used when there are >256 players seen by client
        
        var inGameTime = br.ReadInt32();
        
        using var presencePacket = new ServerPackets();
        
        foreach (var p in session.Players)
            presencePacket.UserPresence(p);

        foreach (var bot in session.Bots)
            presencePacket.BotPresence(bot);
        
        player.Enqueue(presencePacket.GetContent());
        
        return Task.CompletedTask;
    }
}