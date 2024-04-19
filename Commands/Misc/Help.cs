using BanchoNET.Attributes;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("help",
        Privileges.Verified,
        "shows all available commands or displays detailed description of a given command")]
    private string Help(Player player, string commandBase, params string[] args)
    {
        if (args.Length > 0
            && _commandMethodsMap.TryGetValue(args[0], out var cm)
            && player.Privileges.HasAnyPrivilege(cm.Attribute.Privileges))
        {
            var description = cm.Attribute.BriefDescription + cm.Attribute.DetailedDescription;
            
            if (cm.Attribute.Aliases == null)
                return description;

            var aliases = cm.Attribute.Aliases.Aggregate("", (current, alias) => current + alias + ", ");
            return description + "\nAliases/shortcuts: " + aliases[..^2];
        }

        return _commandMethodsMap
            .DistinctBy(c => c.Value.Method)
            .Where(c => player.Privileges.HasAnyPrivilege(c.Value.Attribute.Privileges))
            .OrderBy(c => c.Key)
            .Aggregate("Command not found, available commands [name - description and usage]:\n",
                (current, c) => current + $"{c.Key} - {c.Value.Attribute.BriefDescription}\n")[..^1];
    }
}