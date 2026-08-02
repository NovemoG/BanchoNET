using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using static BanchoNET.Handlers.Stable.Commands.CommandHandlerMap;

namespace BanchoNET.Handlers.Stable.Commands;

public partial class CommandProcessor
{
    [Command("reconnect",
        PlayerPrivileges.Unrestricted,
        "Instantly reconnects player with given username. Syntax: reconnect [<username>]",
        "If you don't have enough permissions this command can only be used to reconnect yourself,\n" +
        "otherwise you can reconnect any player by providing their username.",
        ["rc"])]
    private async Task<string> Reconnect(string[] args)
    {
        if (args.Length == 0)
        {
            await playerCoordinator.LogoutPlayer(_playerCtx);
            return "";
        }

        if (args.Length > 0 && !PlayerExtensions.CanUseCommand(_playerCtx, PlayerPrivileges.Administrator))
            return "Not enough privileges to reconnect other players.";

        var targetPlayer = playerService.GetPlayer(args[0]);
        if (targetPlayer == null)
            return PlayerNotFound;

        if (targetPlayer.IsBot)
            return "Dummy, you can't reconnect a bot \ud83d\udc7c";

        await playerCoordinator.LogoutPlayer(targetPlayer);

        return $"{targetPlayer.Username} has been reconnected.";
    }
}