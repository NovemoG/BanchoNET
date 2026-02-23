using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("delete_user",
        PlayerPrivileges.Developer | PlayerPrivileges.Staff,
        "Deletes provided user's account. Syntax: delete_user <delete_scores> [<force>] \"<username>\"",
        "\nForce is only needed if you want to delete other staff member account or an online player.")]
    private async Task<string> DeleteUser(string[] args)
    {
        if (args.Length == 0)
            return $"No parameters provided. Syntax: {Prefix}delete_user <delete_scores> [<force>] \"<username>\"";
        
        if (args.Length < 2)
            return $"Invalid number of parameters provided. Syntax: {Prefix}delete_user <delete_scores> [<force>] \"<username>\"";
        
        if (!bool.TryParse(args[1], out var deleteScores) || !(deleteScores = args[1] == "1"))
            return "Invalid delete_scores parameter provided. It must be a boolean value.";

        var force = false;
        if (args.Length > 2 && (!bool.TryParse(args[2], out force) || !(force = args[2] == "1")))
            return "Invalid force parameter provided. It must be a boolean value.";
        
        //TODO is it really needed since every nickname is converted to safe anyway?
        var username = args.MergeQuotedElements().Unquote();
        
        var player = await players.GetPlayerInfo(username);
        if (player == null)
            return "Player with provided username does not exist.";
        
        if (((PlayerPrivileges)player.Privileges).GetHighestPrivilege() >= PlayerPrivileges.Moderator && !force)
            return "This is other staff member account. If you're sure that you want to delete it - set force to true.";

        var result = await players.DeletePlayer(player, deleteScores, force);
        
        return result
            ? $"{player.Username}'s account has been successfully deleted."
            : "Player is online. If you're sure of what you're doing - set force to true.";
    }
}