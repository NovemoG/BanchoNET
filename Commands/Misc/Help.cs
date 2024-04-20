using BanchoNET.Attributes;
using BanchoNET.Models;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;
using static BanchoNET.Utils.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("help",
        Privileges.Verified,
        "Shows a list of all available commands or displays detailed description of a given command.")]
    private Task<string> Help(CommandParameters parameters, params string[] args)
    {
        if (args.Length > 0
            && CommandsMap.TryGetValue(args[0], out var command)
            && parameters.Player.Privileges.HasAnyPrivilege(command.Attribute.Privileges))
        {
            var description = command.Attribute.BriefDescription + command.Attribute.DetailedDescription;
            
            if (command.Attribute.Aliases == null)
                return Task.FromResult(description);

            var aliases = command.Attribute.Aliases.Aggregate("", (current, alias) => current + $"{_prefix}{alias}, ");
            return Task.FromResult(description + "\nAliases/shortcuts: " + aliases[..^2]);
        }

        if (args.Length == 0)
        {
            return Task.FromResult(CommandsMap
                .DistinctBy(c => c.Value.Method)
                .Where(c => parameters.Player.Privileges.HasAnyPrivilege(c.Value.Attribute.Privileges))
                .OrderBy(c => c.Key)
                .Aggregate("Here is a list of available commands [name - description and usage]:\n",
                    (current, c) => current + $"{_prefix}{c.Key} - {c.Value.Attribute.BriefDescription}\n")[..^1]);
        }

        return Task.FromResult($"Command not found. Please use '{_prefix}help' to see all available commands.");
    }
}