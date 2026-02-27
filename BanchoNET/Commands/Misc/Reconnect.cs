using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using static BanchoNET.Commands.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("reconnect",
        PlayerPrivileges.Unrestricted,
        "Instantly reconnects player with given username. Syntax: reconnect [<username>]",
        "If you don't have enough permissions this command can only be used to reconnect yourself,\n" +
        "otherwise you can reconnect any player by providing their username.",
        ["rc"])]
    private Task<string> Reconnect(string[] args)
    {
        if (args.Length == 0)
        {
            playerCoordinator.LogoutPlayer(_playerCtx);
            return Task.FromResult("");
        }

        if (args.Length > 0 && !_playerCtx.CanUseCommand(PlayerPrivileges.Administrator))
            return Task.FromResult("Not enough privileges to reconnect other players.");
        
        var targetPlayer = playerService.GetPlayer(args[0]);
        if (targetPlayer == null)
            return Task.FromResult(PlayerNotFound);
        
        if (targetPlayer.IsBot)
            return Task.FromResult("Dummy, you can't reconnect a bot \ud83d\udc7c");
            
        playerCoordinator.LogoutPlayer(targetPlayer);

        return Task.FromResult($"{targetPlayer.Username} has been reconnected.");
    }
}