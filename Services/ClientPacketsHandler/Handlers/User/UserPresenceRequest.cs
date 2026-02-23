using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task UserPresenceRequest(Player player, BinaryReader br)
    {
        var userIds = br.ReadOsuListInt32();
        
        foreach (var id in userIds)
        {
            var target = session.GetPlayerById(id);
            if (target == null) continue;
            
            using var presencePacket = new ServerPackets();
            
            if (target.IsBot) presencePacket.BotPresence(target);
            else presencePacket.UserPresence(target);
            
            player.Enqueue(presencePacket.GetContent());
        }
        
        return Task.CompletedTask;
    }
}