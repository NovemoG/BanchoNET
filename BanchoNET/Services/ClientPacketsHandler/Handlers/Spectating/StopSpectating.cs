using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task StopSpectating(User player, BinaryReader br)
    {
        var host = player.Spectating;
        if (host == null)
        {
            Console.WriteLine($"{player.Username} tried to stop spectating when not spectating anyone");

            return Task.CompletedTask;
        }
        
        playerCoordinator.RemoveSpectator(host, player);

        return Task.CompletedTask;
    }
}