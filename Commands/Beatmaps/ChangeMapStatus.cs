using BanchoNET.Attributes;
using BanchoNET.Models;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("map",
        Privileges.Alumni | Privileges.Nominator | Privileges.Administrator | Privileges.Developer,
        "Changes the status of previously /np'd map. Syntax: map <status> <map/set>",
        "\nAvailable statuses: loved, qualified, approved, ranked, pending/unrank (does the same)" +
        "\nmrs - ranks whole set",
        ["mrs"])]
    private static string ChangeMapStatus(CommandParameters parameters, params string[] args)
    {
        if (args.Length == 0 && parameters.CommandBase != "mrs")
            return "No parameter provided. Use '!help map' for more information.";
        
        return "";
    }

    private static string ChangeStatus()
    {
        return "";
    }
}