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
    private string Help(CommandParameters parameters, params string[] args)
    {
        if (args.Length > 0
            && CommandsMap.TryGetValue(args[0], out var command)
            && parameters.Player.Privileges.HasAnyPrivilege(command.Attribute.Privileges))
        {
            var description = command.Attribute.BriefDescription + command.Attribute.DetailedDescription;
            
            if (command.Attribute.Aliases == null)
                return description;

            var aliases = command.Attribute.Aliases.Aggregate("", (current, alias) => current + $"{AppSettings.CommandPrefix}{alias}, ");
            return description + "\nAliases/shortcuts: " + aliases[..^2];
        }

        return CommandsMap
            .DistinctBy(c => c.Value.Method)
            .Where(c => parameters.Player.Privileges.HasAnyPrivilege(c.Value.Attribute.Privileges))
            .OrderBy(c => c.Key)
            .Aggregate(
                $"{(args.Length == 0
                    ? ""
                    : "Command not found. ")}" +
                $"Here is a list of available commands [name - description and usage]:\n",
                (current, c) =>
                    current + $"{AppSettings.CommandPrefix}{c.Key} - {c.Value.Attribute.BriefDescription}\n")[..^1];
    }
}