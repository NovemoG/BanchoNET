using BanchoNET.Attributes;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Commands;

public class ChangeMapStatusCommand
{
    [Command("map",
        Privileges.Alumni | Privileges.Nominator | Privileges.Administrator | Privileges.Developer,
        "changes the status of previously /np'd map. Syntax: map <status> <map/set>",
        "\nAvailable statuses: loved, qualified, approved, ranked, pending/unrank (does the same)" +
        "\nmrs - ranks whole set",
        ["mrs"])]
    private static string ChangeMapStatus(Player player, string commandBase, params string[] args)
    {
        if (args.Length == 0 && commandBase != "mrs")
            return "No parameter provided. Use '!help map' for more information.";
        
        return "";
    }

    private static string ChangeStatus()
    {
        return "";
    }
}