using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("change_username",
        PlayerPrivileges.Administrator,
        "Changes provided player's username to a desired one. Syntax: change_username <old_username>/<new_username> or just <new_username>",
        "\nIt must not be taken or disallowed and it must be a valid one: have between 2 and 15 characters, contain " +
        "only alphanumeric characters, underscores, spaces, dashes or square brackets, and not start or end with a space.",
        ["cu"])]
    private async Task<string> ChangeUsername(string[] args)
    {
        if (args.Length == 0)
            return $"No parameters provided. Syntax: {Prefix}change_username <old_username>/<new_username>";
        
        var usernames = string.Join(' ', args).Split('/', 2);
        var changingSelf = usernames.Length == 1;
        var newName = usernames[^1];
        
        if (!changingSelf && !await players.PlayerExists(usernames[0]))
            return "Player with provided username does not exist.";
        
        if (!Regexes.Username.Match(newName).Success)
            return "Username must be between 2 and 15 characters, contain only alphanumeric characters, underscores, " +
                   "spaces, dashes or square brackets, and not start or end with a space.";

        if (newName.Contains(' ') && newName.Contains('_'))
            return "Username must not contain both spaces and underscores.";
        
        if (await players.UsernameTaken(newName))
            return "Username already taken by other player.";
        
        if (AppSettings.DisallowedNames.Contains(newName))
            return "Username is disallowed.";

        return await players.ChangeUsername(changingSelf ? _playerCtx.Username : usernames[0], newName)
            ? $"{(changingSelf ? _playerCtx.Username : usernames[0])}'s username has been successfully changed to {newName}. Please relog to see changes."
            : "An error occurred while changing the username.";
    }
}