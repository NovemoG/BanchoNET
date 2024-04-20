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
        "\nmp clearrefs - Removes all referees from the lobby (aside of creator)." +
        "\n        Only the creator of the lobby can manage referees." +
        "\nmp listrefs - Lists all referees in the lobby" +
        "\nmp close - Disbands current lobby." +
        "\nParameters inside of [] are optional.")]
    private async Task<string> Multiplayer(CommandParameters parameters, params string[] args)
    {
        var player = parameters.Player;
        var prefix = AppSettings.CommandPrefix;
        
        if (args.Length == 0)
            return $"No parameter(s) provided. Check available options using '{prefix}mp help' or '{prefix}help mp'.";

        if (player.Lobby == null && args[0] is not ("help" or "create"))
            return "You're not in a multiplayer lobby. Use 'mp create [<name>] [<password>]' to create one.";

        return args[0] switch
        {
            "help" => await Help(new CommandParameters
            {
                Player = player
            }, "mp"),
            "create" => CreateMultiplayerLobby(player, args[1..]),
            "invite" => InviteToLobby(player, args[1..]),
            "name" => ChangeLobbyName(player, args[1..]),
            "password" => ChangeLobbyPassword(player, args[1..]),
            "lock" => LockLobby(player, args[1..]),
            "unlock" => UnlockLobby(player, args[1..]),
            "size" => SetLobbySize(player, args[1..]),
            "set" => SetLobbyProperties(player, args[1..]),
            "move" => MovePlayer(player, args[1..]),
            "host" => TransferHost(player, args[1..]),
            "clearhost" => ClearHost(player, args[1..]),
            "abort" => AbortMatch(player, args[1..]),
            "team" => MovePlayerToTeam(player, args[1..]),
            "map" => ChangeBeatmap(player, args[1..]),
            "mods" => ChangeMods(player, args[1..]),
            "start" => StartMatch(player, args[1..]),
            "timer" => StartTimer(player, args[1..]),
            "aborttimer" => AbortTimer(player, args[1..]),
            "kick" => KickPlayer(player, args[1..]),
            "ban" => BanPlayer(player, args[1..]),
            "addref" => AddReferee(player, args[1..]),
            "removeref" => RemoveReferee(player, args[1..]),
            "clearrefs" => ClearReferees(player, args[1..]),
            "listrefs" => ListReferees(player, args[1..]),
            "close" => CloseLobby(player, args[1..]),
            _ => $"Invalid parameter provided. Check available options using '{prefix}mp help' or '{prefix}help mp'."
        };
    }

    private string CreateMultiplayerLobby(Player player, params string[] args)
    {
        return "";
    }
    
    private string InviteToLobby(Player player, params string[] args)
    {
        return "";
    }
    
    private string ChangeLobbyName(Player player, params string[] args)
    {
        return "";
    }
    
    private string ChangeLobbyPassword(Player player, params string[] args)
    {
        return "";
    }
    
    private string LockLobby(Player player, params string[] args)
    {
        return "";
    }
    
    private string UnlockLobby(Player player, params string[] args)
    {
        return "";
    }
    
    private string SetLobbySize(Player player, params string[] args)
    {
        return "";
    }
    
    private string SetLobbyProperties(Player player, params string[] args)
    {
        return "";
    }
    
    private string MovePlayer(Player player, params string[] args)
    {
        return "";
    }
    
    private string TransferHost(Player player, params string[] args)
    {
        return "";
    }
    
    private string ClearHost(Player player, params string[] args)
    {
        return "";
    }
    
    private string AbortMatch(Player player, params string[] args)
    {
        return "";
    }
    
    private string MovePlayerToTeam(Player player, params string[] args)
    {
        return "";
    }
    
    private string ChangeBeatmap(Player player, params string[] args)
    {
        return "";
    }
    
    private string ChangeMods(Player player, params string[] args)
    {
        return "";
    }
    
    private string StartMatch(Player player, params string[] args)
    {
        return "";
    }
    
    private string StartTimer(Player player, params string[] args)
    {
        return "";
    }
    
    private string AbortTimer(Player player, params string[] args)
    {
        return "";
    }
    
    private string KickPlayer(Player player, params string[] args)
    {
        return "";
    }
    
    private string BanPlayer(Player player, params string[] args)
    {
        return "";
    }
    
    private string AddReferee(Player player, params string[] args)
    {
        return "";
    }
    
    private string RemoveReferee(Player player, params string[] args)
    {
        return "";
    }
    
    private string ClearReferees(Player player, params string[] args)
    {
        return "";
    }
    
    private string ListReferees(Player player, params string[] args)
    {
        return "";
    }
    
    private string CloseLobby(Player player, params string[] args)
    {
        return "";
    }
}