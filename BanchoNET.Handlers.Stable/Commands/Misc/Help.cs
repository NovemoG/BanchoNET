using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using static BanchoNET.Handlers.Stable.Commands.CommandHandlerMap;

namespace BanchoNET.Handlers.Stable.Commands;

public partial class CommandProcessor
{
    [Command("help",
        PlayerPrivileges.Unrestricted,
        "Shows a list of all available commands or displays detailed description of a given command.")]
    private Task<string> Help(string[] args)
    {
        if (args.Length > 0
            && CommandsMap.TryGetValue(args[0], out var command)
            && PlayerExtensions.CanUseCommand(_playerCtx, command.Attribute.Privileges))
        {
            var description = command.Attribute.BriefDescription + command.Attribute.DetailedDescription;
            
            if (command.Attribute.Aliases == null)
                return Task.FromResult(description);

            var aliases = command.Attribute.Aliases.Aggregate("", (current, alias) => current + $"{Prefix}{alias}, ");
            return Task.FromResult(description + "\nAliases/shortcuts: " + aliases[..^2]);
        }

        if (args.Length == 0)
        {
            return Task.FromResult(CommandsMap
                .DistinctBy(c => c.Value.Method)
                .Where(c => PlayerExtensions.CanUseCommand(_playerCtx, c.Value.Attribute.Privileges))
                .OrderBy(c => c.Key)
                .Aggregate("Here is a list of available commands [name - description and usage]:\n",
                    (current, c) => current + $"{Prefix}{c.Key} - {c.Value.Attribute.BriefDescription}\n")[..^1]);
        }

        return Task.FromResult(CommandNotFound);
    }
}