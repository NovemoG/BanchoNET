using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("reconnect",
        Privileges.Unrestricted,
        "Instantly reconnects player with given username. Syntax: reconnect [<username>]",
        "If you don't have enough permissions this command can only be used to reconnect yourself,\n" +
        "otherwise you can reconnect any player by providing their username.",
        ["rc"])]
    private Task<string> Reconnect(params string[] args)
    {
        if (args.Length == 0)
            _session.LogoutPlayer(_playerCtx);

        if (args.Length != 1 || !_playerCtx.CanUseCommand(Privileges.Administrator))
            return Task.FromResult("Not enough privileges to reconnect other players.");
        
        var targetPlayer = _session.GetPlayer(username: args[0]);
        if (targetPlayer == null)
            return Task.FromResult("Target player not found.");
        
        if (targetPlayer.IsBot)
            return Task.FromResult("Dummy, you can't reconnect a bot \ud83d\udc7c.");
            
        _session.LogoutPlayer(targetPlayer);

        return Task.FromResult($"{targetPlayer.Username} has been reconnected.");
    }
}