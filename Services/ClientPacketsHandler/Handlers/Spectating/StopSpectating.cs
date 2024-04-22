﻿using BanchoNET.Objects.Players;
using BanchoNET.Utils;

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