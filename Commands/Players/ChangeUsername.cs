using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("change_username",
        Privileges.Administrator,
        "Changes provided player's username to a desired one. Syntax: change_username <old_username>/<new_username>",
        "\nIt must not be taken or disallowed and it must be a valid one: have between 2 and 15 characters, contain " +
        "only alphanumeric characters, underscores, spaces, dashes or square brackets, and not start or end with a space.",
        ["cu"])]
    private async Task<string> ChangeUsername(string[] args)
    {
        if (args.Length == 0)
            return $"No parameters provided. Syntax: {Prefix}change_username <old_username>/<new_username>";
        
        var usernames = string.Join(' ', args).Split('/', 2);
        if (usernames.Length is < 2)
            return $"Invalid number of parameters provided. {Prefix}Syntax: change_username <old_username>/<new_username>";

        var player = await players.FetchPlayerInfoByName(usernames[0]);
        if (player == null)
            return "Player with provided username does not exist.";
        
        if (!Regexes.Username.Match(usernames[1]).Success)
            return "Username must be between 2 and 15 characters, contain only alphanumeric characters, underscores, " +
                   "spaces, dashes or square brackets, and not start or end with a space.";

        if (usernames[1].Contains(' ') && usernames[1].Contains('_'))
            return "Username must not contain both spaces and underscores.";
        
        if (await players.UsernameTaken(usernames[1]))
            return "Username already taken by other player.";
        
        if (AppSettings.DisallowedNames.Contains(usernames[1]))
            return "Username is disallowed.";

        return await players.ChangeUsername(player, usernames[1])
            ? $"{player.Username}'s username has been successfully changed to {usernames[1]}."
            : "An error occurred while changing the username.";
    }
}