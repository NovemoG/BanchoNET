using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using static BanchoNET.Handlers.Stable.Commands.CommandHandlerMap;

namespace BanchoNET.Handlers.Stable.Commands;

public partial class CommandProcessor
{
    [Command("restrict",
        PlayerPrivileges.Moderator | PlayerPrivileges.Staff,
        "Restricts provided user's account. Syntax: restrict <username> <reason>",
        "\nReason can be provided with spaces between words.")]
    private async Task<string> Restrict(string[] args)
    {
        if (args.Length == 0)
            return $"No parameters provided. Syntax: {Commands.CommandProcessor.Prefix}restrict <username> <reason>";
        
        if (args.Length < 2)
            return "You must provide a reason for a restriction.";
        
        var username = args[0];
        var reason = string.Join(" ", args[1..]);
        
        var player = await players.GetPlayerOrOffline(username);
        if (player == null) return PlayerNotFound;
        
        if (player.IsBot && !EnumExtensions.HasPrivilege(player.Privileges, PlayerPrivileges.Staff))
            return "You can't restrict a bot.";

        if (!EnumExtensions.HasPrivilege(player.Privileges, PlayerPrivileges.Verified)
            || !EnumExtensions.HasPrivilege(player.Privileges, PlayerPrivileges.Unrestricted))
            return "This player is already restricted.";
        
        if (EnumExtensions.GetHighestPrivilege((PlayerPrivileges)player.Privileges) >= EnumExtensions.GetHighestPrivilege((PlayerPrivileges)_playerCtx.Privileges))
            return "You can't restrict an account that has higher or equal privileges than you.";

        var result = await players.RestrictPlayer(player, reason);
        
        return result
            ? $"{username}'s account has been successfully restricted."
            : "By some miracle a player with provided username couldn't be found in database.";
    }

    [Command("unrestrict",
        PlayerPrivileges.Moderator | PlayerPrivileges.Staff,
        "Removes a restriction from provided user's account. Syntax: unrestrict <username> <reason>",
        "\nReason can be provided with spaces between words.")]
    private async Task<string> Unrestrict(string[] args)
    {
        if (args.Length == 0)
            return $"No parameters provided. Syntax: {Commands.CommandProcessor.Prefix}unrestrict <username> <reason>";
        
        if (args.Length < 2)
            return "You must provide a reason for removing a restriction.";
        
        var username = args[0];
        var reason = string.Join(" ", args[1..]);

        var player = await players.GetPlayerOrOffline(username);
        if (player == null) return PlayerNotFound;

        if (EnumExtensions.HasPrivilege(player.Privileges, PlayerPrivileges.Verified)
            || EnumExtensions.HasPrivilege(player.Privileges, PlayerPrivileges.Unrestricted))
            return "This player is not restricted";

        var result = await players.UnrestrictPlayer(player, reason);
        
        return result
            ? $"{username}'s account has been successfully unrestricted."
            : "By some miracle a player with provided username couldn't be found in database.";
    }
}