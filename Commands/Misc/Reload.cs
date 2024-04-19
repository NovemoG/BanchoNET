using BanchoNET.Attributes;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Commands;

public class ReloadCommand
{
    [Command("reload",
        Privileges.Staff,
        "reloads given collection/configuration. Syntax: reload <collection>",
        "\nAvailable options: commands",
        ["r"])]
    private static string Reload(Player player, string commandBase, params string[] args)
    {
        if (args.Length == 0)
            return "No parameter provided. Available options are: commands";
        
        return args[0] switch
        {
            "commands" => ReloadCommands(),
            _ => "Invalid parameter provided. Available options are: commands"
        };
    }

    private static string ReloadCommands()
    {
        var elapsed = CommandProcessor.ReloadCommands();
        
        return $"Commands successfully reloaded in: {elapsed.Milliseconds}ms {elapsed.Microseconds}μs";
    }
}