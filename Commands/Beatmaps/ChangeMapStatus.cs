using BanchoNET.Attributes;

namespace BanchoNET.Commands;

public class ChangeMapStatusCommand
{
    [Command("map",
        "changes the status of previously /np'd map. Syntax: map <status> <map/set>",
        "\nAvailable statuses: loved, qualified, approved, ranked, pending/unrank (does the same)" +
        "\nmrs - ranks whole set",
        ["mrs"])]
    private static string ChangeMapStatus(params string[] args)
    {
        if (args.Length == 0)
            return "No parameter provided. Use '!help map' for more information.";
        
        return "";
    }

    private static string ChangeStatus()
    {
        return "";
    }
}