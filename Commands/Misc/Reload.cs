using BanchoNET.Attributes;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("reload",
        Privileges.Staff,
        "reloads given collection/configuration. Syntax: reload <collection>",
        "\nAvailable options: commands",
        ["r"])]
    private string Reload(Player player, string commandBase, params string[] args)
    {
        if (args.Length == 0)
            return "No parameter provided. Available options are: commands";
        
        return args[0] switch
        {
            "commands" => ReloadCommandsCollection(),
            _ => "Invalid parameter provided. Available options are: commands"
        };
    }

    private string ReloadCommandsCollection()
    {
        var elapsed = ReloadCommands();
        var returnString = "Commands successfully reloaded in:";
        
        if (elapsed.Milliseconds > 0)
            returnString += $" {elapsed.Milliseconds}ms";
        
        return $"{returnString} {elapsed.Microseconds}μs";
    }
}