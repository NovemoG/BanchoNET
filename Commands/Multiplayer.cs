using BanchoNET.Attributes;
using BanchoNET.Models;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("mp",
        Privileges.Verified,
        "A set of commands to manage your multiplayer lobby. List of commands available under 'mp help' or 'help mp' command.",
        "\nmp create [<name>] [<password>] - Creates a multiplayer lobby with a given name." +
        "\nmp invite <username> - Invites a player with a given username to your current multiplayer lobby." +
        "\nmp name <name> - Changes the name of your current multiplayer lobby." +
        "\nmp password [<password>] - Changes the password of your current multiplayer lobby. If <password> is not " +
        "provided it will be removed." +
        "\nmp lock - Locks the lobby so that players can’t change their team and slot." +
        "\nmp unlock - Reverses the above." +
        "\nmp size <size> - Sets the amount of available slots (1-16) in the lobby." +
        "\nmp set <teammode> [<scoremode>] [<size>] - Sets various lobby properties." +
        "\n        0: Head To Head, 1: Tag Coop, 2: Team Vs, 3: Tag Team Vs"+
        "\n        0: Score, 1: Accuracy, 2: Combo, 3: Score V2" +
        "\nmp move <username> <slot> - Moves a player within the lobby to the specified 1-indexed slot." +
        "\nmp host <username> - Transfers host to the player." +
        "\nmp clearhost - Clears the lobby host." +
        "\nmp abort - Aborts the match." +
        "\nmp team <username> <color> - Moves a player to the specified team." +
        "\nmp map <mapid> [<gamemode>] - Changes the beatmap and gamemode of the lobby." +
        "\n        gamemode - 0: osu!, 1: Taiko, 2: Catch The Beat, 3: osu!Mania" +
        "\nmp mods <mod> [<mod>] ... - Removes all currently applied mods and applies these mods to the lobby." +
        "\nmp start [<seconds>] - Starts the match after a set time (in seconds) or instantaneously (if used by host " +
        "or all players are ready) if time is not present." +
        "\nmp timer [<seconds>] - Begins a countdown timer. Default is 30s." +
        "\nmp aborttimer - Stops the current timer (both normal timers and match start timer)" +
        "\nTimer announcements occur every minute, 30s, 15s, 10s, 5s and earlier." +
        "\nmp kick <username> - Kicks the player from the lobby" +
        "\nmp ban <username> - Bans the player from the lobby" +
        "\nmp addref <username> [<username>] ... - Adds a referee to the lobby." +
        "\nmp removeref <username> [<username>] ... - Removes a referee from the lobby." +
        "\nmp removerefs - Removes all referees from the lobby (aside of creator)." +
        "\n        Only the creator of the lobby can manage referees." +
        "\nmp listrefs - Lists all referees in the lobby" +
        "\nmp close - Disbands current lobby." +
        "\nParameters inside of [] are optional.")]
    private string Multiplayer(CommandParameters parameters, params string[] args)
    {
        var player = parameters.Player;
        var prefix = AppSettings.CommandPrefix;
        
        if (args.Length == 0)
            return $"No parameter provided. Check available options using '{prefix}mp help' or '{prefix}help mp'.";

        if (player.Lobby == null && args[0] is not ("help" or "create"))
            return "You're not in a multiplayer lobby. Use 'mp create [<name>] [<password>]' to create one.";

        return args[0] switch
        {
            "help" => Help(new CommandParameters
            {
                Player = player
            }, "mp"),
            "create" => MakeMultiplayerLobby(player, args[1..]),
            _ => $"Invalid parameter provided. Check available options using '{prefix}mp help' or '{prefix}help mp'."
        };
    }

    private string MakeMultiplayerLobby(Player player, params string[] args)
    {
        return "";
    }
}