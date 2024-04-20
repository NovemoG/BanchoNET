using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("reload",
        Privileges.Staff,
        "Reloads given collection/configuration. Syntax: reload <collection>",
        "\nAvailable options: commands",
        ["r"])]
    private Task<string> Reload(params string[] args)
    {
        if (args.Length == 0)
            return Task.FromResult("No parameter provided. Available options: commands");
        
        return Task.FromResult(args[0] switch
        {
            "commands" => ReloadCommandsCollection(),
            _ => $"Invalid parameter provided. Available options: commands."
        });
    }

    private static string ReloadCommandsCollection()
    {
        var elapsed = CommandHandlerMap.ReloadCommands();
        var returnString = "Commands successfully reloaded in:";
        
        if (elapsed.Milliseconds > 0)
            returnString += $" {elapsed.Milliseconds}ms";
        
        return $"{returnString} {elapsed.Microseconds}μs";
    }
}