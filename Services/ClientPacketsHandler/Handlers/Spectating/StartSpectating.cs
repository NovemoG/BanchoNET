using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task StartSpectating(Player player, BinaryReader br)
    {
        var targetId = br.ReadInt32();
        var target = _session.GetPlayerById(targetId);

        if (target == null)
        {
            Console.WriteLine("Tried spectating a player that doesn't exist");

            return Task.CompletedTask;
        }

        var currentHost = player.Spectating;
        if (currentHost != null)
        {
            if (currentHost == target)
            {
                using var spectatorPacket = new ServerPackets();
                spectatorPacket.SpectatorJoined(player.Id);
                target.Enqueue(spectatorPacket.GetContent());

                using var playerJoinedPacket = new ServerPackets();
                playerJoinedPacket.FellowSpectatorJoined(player.Id);
                var bytes = playerJoinedPacket.GetContent();
                
                foreach (var spectator in target.Spectators.Where(spectator => spectator != player))
                    spectator.Enqueue(bytes);

                return Task.CompletedTask;
            }
            
            currentHost.RemoveSpectator(player);
        }
        
        target.AddSpectator(player);
        
        return Task.CompletedTask;
    }
}