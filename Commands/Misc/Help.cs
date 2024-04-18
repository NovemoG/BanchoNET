using BanchoNET.Attributes;

namespace BanchoNET.Commands;

public class HelpCommand
{
    [Command("help", "shows all available commands or displays detailed description of a given command")]
    private static string Help(params string[] args)
    {
        if (args.Length > 0 && CommandProcessor.CommandDescriptions.TryGetValue(args[0], out var description))
            return description.Brief + description.Detailed;

        return CommandProcessor.CommandDescriptions
            .OrderBy(info => info.Key)
            .Aggregate("Available commands [name - description and usage]:\n", (current, info) => current + $"{info.Key} - {info.Value.Brief}\n")[..^1];
    }
}