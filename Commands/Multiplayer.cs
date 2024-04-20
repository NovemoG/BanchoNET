using BanchoNET.Attributes;
using BanchoNET.Objects.Multiplayer;
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
    private async Task<string> Multiplayer(params string[] args)
    {
        var prefix = AppSettings.CommandPrefix;
        
        if (args.Length == 0)
            return $"No parameter(s) provided. Check available options using '{prefix}mp help' or '{prefix}help mp'.";

        if (_playerCtx.Lobby == null && args[0] is not ("help" or "create"))
            return "You're not in a multiplayer lobby. Use 'mp create [<name>] [<password>]' to create one.";

        if (_playerCtx.Lobby != null && _playerCtx.Lobby.Chat != _channelCtx)
            return "";

        return args[0] switch
        {
            "help" => await Help("mp"),
            "create" => await CreateMultiplayerLobby(args[1..]),
            "invite" => InviteToLobby(args[1..]),
            "name" => ChangeLobbyName(args[1..]),
            "password" => ChangeLobbyPassword(args[1..]),
            "lock" => LockLobby(args[1..]),
            "unlock" => UnlockLobby(args[1..]),
            "size" => SetLobbySize(args[1..]),
            "set" => SetLobbyProperties(args[1..]),
            "move" => MovePlayer(args[1..]),
            "host" => TransferHost(args[1..]),
            "clearhost" => ClearHost(args[1..]),
            "abort" => AbortMatch(args[1..]),
            "team" => MovePlayerToTeam(args[1..]),
            "map" => ChangeBeatmap(args[1..]),
            "mods" => ChangeMods(args[1..]),
            "start" => StartMatch(args[1..]),
            "timer" => StartTimer(args[1..]),
            "aborttimer" => AbortTimer(args[1..]),
            "kick" => KickPlayer(args[1..]),
            "ban" => BanPlayer(args[1..]),
            "addref" => AddReferee(args[1..]),
            "removeref" => RemoveReferee(args[1..]),
            "clearrefs" => ClearReferees(args[1..]),
            "listrefs" => ListReferees(args[1..]),
            "close" => CloseLobby(args[1..]),
            _ => $"Invalid parameter provided. Check available options using '{prefix}mp help' or '{prefix}help mp'."
        };
    }

    private async Task<string> CreateMultiplayerLobby(params string[] args)
    {
        if (_playerCtx.Lobby != null)
            return "";
        
        var lobby = new MultiplayerLobby
        {
            Name = args.Length == 1 ? args[0] : $"{_playerCtx.Username}'s match",
            Password = args.Length == 2 ? args[1] : "",
            HostId = _playerCtx.Id,
            Freemods = true,
            BeatmapId = _playerCtx.Status.BeatmapId,
            BeatmapMD5 = _playerCtx.Status.BeatmapMD5,
            BeatmapName = "",
            Seed = Random.Shared.Next(),
            Slots =
            {
                [0] = new MultiplayerSlot
                {
                    Player = _playerCtx,
                    Status = SlotStatus.NotReady
                }
            }
        };

        foreach (var slot in lobby.Slots[1..])
            slot.Status = SlotStatus.Open;
        
        MultiplayerExtensions.CreateLobby(lobby, _playerCtx, await multiplayer.GetMatchId());
        
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string InviteToLobby(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";

        if (args.Length == 0)
            return $"No username provided. Use '{_prefix}mp invite <username>'.";
        
        return "";
    }
    
    private string ChangeLobbyName(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string ChangeLobbyPassword(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string LockLobby(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string UnlockLobby(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string SetLobbySize(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string SetLobbyProperties(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string MovePlayer(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string TransferHost(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string ClearHost(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string AbortMatch(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string MovePlayerToTeam(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string ChangeBeatmap(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string ChangeMods(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string StartMatch(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string StartTimer(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string AbortTimer(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string KickPlayer(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string BanPlayer(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string AddReferee(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string RemoveReferee(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string ClearReferees(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string ListReferees(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
    
    private string CloseLobby(params string[] args)
    {
        if (_playerCtx.Lobby == null)
            return "";
        
        return "";
    }
}