using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task StartSpectating(User player, BinaryReader br)
    {
        var targetId = br.ReadInt32();
        var target = playerService.GetPlayer(targetId);
        if (target == null)
        {
            Console.WriteLine("Tried spectating a player that doesn't exist");

            return Task.CompletedTask;
        }

        var currentHost = player.Spectating;
        if (currentHost != null)
        {
            if (currentHost.Equals(target))
            {
                target.Enqueue(new ServerPackets()
                    .SpectatorJoined(player.Id)
                    .FinalizeAndGetContent());
                
                var bytes = new ServerPackets()
                    .FellowSpectatorJoined(player.Id)
                    .FinalizeAndGetContent();
                
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