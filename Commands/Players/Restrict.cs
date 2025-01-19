using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils.Extensions;
using static BanchoNET.Utils.Maps.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("restrict",
        PlayerPrivileges.Moderator | PlayerPrivileges.Staff,
        "Restricts provided user's account. Syntax: restrict <username> <reason>",
        "\nReason can be provided with spaces between words.")]
    private async Task<string> Restrict(string[] args)
    {
        if (args.Length == 0)
            return $"No parameters provided. Syntax: {Prefix}restrict <username> <reason>";
        
        if (args.Length < 2)
            return "You must provide a reason for a restriction.";
        
        var username = args[0];
        var reason = string.Join(" ", args[1..]);
        
        var player = await players.GetPlayerOrOffline(username);
        if (player == null) return PlayerNotFound;
        
        if (player.IsBot && !player.Privileges.HasPrivilege(PlayerPrivileges.Staff))
            return "You can't restrict a bot.";

        if (!player.Privileges.HasPrivilege(PlayerPrivileges.Verified)
            || !player.Privileges.HasPrivilege(PlayerPrivileges.Unrestricted))
            return "This player is already restricted.";
        
        if (player.Privileges.GetHighestPrivilege() >= _playerCtx.Privileges.GetHighestPrivilege())
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
            return $"No parameters provided. Syntax: {Prefix}unrestrict <username> <reason>";
        
        if (args.Length < 2)
            return "You must provide a reason for removing a restriction.";
        
        var username = args[0];
        var reason = string.Join(" ", args[1..]);

        var player = await players.GetPlayerOrOffline(username);
        if (player == null) return PlayerNotFound;

        if (player.Privileges.HasPrivilege(PlayerPrivileges.Verified)
            || player.Privileges.HasPrivilege(PlayerPrivileges.Unrestricted))
            return "This player is not restricted";

        var result = await players.UnrestrictPlayer(player, reason);
        
        return result
            ? $"{username}'s account has been successfully unrestricted."
            : "By some miracle a player with provided username couldn't be found in database.";
    }
}