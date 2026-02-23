using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task StopSpectating(Player player, BinaryReader br)
    {
        var host = player.Spectating;

        if (host == null)
        {
            Console.WriteLine($"{player.Username} tried to stop spectating when not spectating anyone");

            return Task.CompletedTask;
        }
        
        host.RemoveSpectator(player);

        return Task.CompletedTask;
    }
}