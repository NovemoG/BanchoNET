using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils.Extensions;
using static BanchoNET.Utils.Maps.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("silence",
        Privileges.Moderator | Privileges.Staff,
        "Silences provided user's account for a specified amount of time. Syntax: silence <username> <duration> <reason>",
        "\nDuration can be provided as follows: 7d - 7 days, 6h30m - 6 hours 30 minutes, 1m5d - 1 month 5 days, 6000 - seconds." +
        "\nYou can connect these timespans however you want (not counting the only seconds one). Other available is y for years." +
        "\nReason can be provided with spaces between words.")]
    private async Task<string> Silence(string[] args)
    {
        if (args.Length == 0)
            return $"No parameters provided. Syntax: {_prefix}silence <username> <duration> <reason>";
        
        if (args.Length < 3)
            return $"Invalid number of parameters provided. Syntax: {_prefix}silence <username> <duration> <reason>";

        var username = args[0];
        var duration = TimeSpan.Zero; //TODO cool time parser
        var reason = string.Join(" ", args[2..]);

        var player = await players.GetPlayerOrOffline(username);
        if (player == null) return PlayerNotFound;
        
        if (player.IsBot)
            return "You can't silence a bot.";
        
        if (player.Privileges.GetHighestPrivilege() >= _playerCtx.Privileges.GetHighestPrivilege())
            return "You can't silence an account that has higher or equal privileges than you.";

        var result = await players.SilencePlayer(player, duration, reason);
        
        return result
            ? $"{username}'s account has been successfully silenced."
            : "By some miracle a player with provided username couldn't be found in database.";
    }

    [Command("unsilence",
        Privileges.Moderator | Privileges.Staff,
        "Removes a silence from provided user's account. Syntax: unsilence <username> <reason>",
        "\nReason can be provided with spaces between words.")]
    private async Task<string> Unsilence(string[] args)
    {
        if (args.Length == 0)
            return $"No parameters provided. Syntax: {_prefix}unsilence <username> <reason>";
        
        if (args.Length < 2)
            return "You must provide a reason for removing a silence status.";
        
        var username = args[0];
        var reason = string.Join(" ", args[1..]);

        var player = await players.GetPlayerOrOffline(username);
        if (player == null) return PlayerNotFound;

        if (player.RemainingSilence < DateTime.Now)
            return "Player is not silenced.";

        var result = await players.UnsilencePlayer(player, reason);
        
        return result
            ? $"{username}'s account has been successfully unsilenced."
            : "By some miracle a player with provided username couldn't be found in database.";
    }
}