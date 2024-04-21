using BanchoNET.Attributes;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    private MultiplayerLobby _lobby;
    
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
        "\nmp rmref <username> [<username>] ... - Removes a referee from the lobby." +
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

        _lobby = _playerCtx.Lobby!;

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
            "aborttimer" => AbortTimer(),
            "kick" => KickPlayer(args[1..]),
            "ban" => BanPlayer(args[1..]),
            "addref" => await AddReferee(args[1..]),
            "rmref" => await RemoveReferee(args[1..]),
            "listrefs" => ListReferees(),
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
        
        return "";
    }
    
    private string InviteToLobby(params string[] args)
    {
        if (args.Length == 0)
            return $"No username provided. Use '{_prefix}mp invite <username>'.";

        var target = _session.GetPlayer(username: args[0]);
        if (target == null)
            return $"{args[0]} is either offline or you misspelled the username.";
        
        MultiplayerExtensions.InviteToLobby(_playerCtx, target);
        
        return $"Invite sent to {target.Username}.";
    }
    
    private string ChangeLobbyName(params string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        if (args.Length == 0)
            return $"No name provided. Use '{_prefix}mp name <name>'.";
        
        _lobby.Name = args[0];
        _lobby.EnqueueState();
        
        Console.WriteLine($"[ChangeLobbyName] {_playerCtx.Username} changed the match name to {args[0]} in lobby with id {_lobby.Id}.");
        
        return $"Match name changed to {args[0]}";
    }
    
    private string ChangeLobbyPassword(params string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        if (args.Length == 0)
        {
            _lobby.Password = "";
            _lobby.EnqueueState();
            return "Password removed.";
        }

        if (args[0].Length < 4)
            return "Password must be at least 4 characters long.";
        
        _lobby.Password = args[0];
        _lobby.EnqueueState();
        
        Console.WriteLine($"[ChangeLobbyPassword] {_playerCtx.Username} changed match password to {args[0]} in lobby with id {_lobby.Id}.");
        return $"Match password changed to {args[0]}.";
    }
    
    private string LockLobby(params string[] args)
    {
        return "";
    }
    
    private string UnlockLobby(params string[] args)
    {
        return "";
    }
    
    private string SetLobbySize(params string[] args)
    {
        return "";
    }
    
    private string SetLobbyProperties(params string[] args)
    {
        return "";
    }
    
    private string MovePlayer(params string[] args)
    {
        return "";
    }
    
    private string TransferHost(params string[] args)
    {
        return "";
    }
    
    private string ClearHost(params string[] args)
    {
        return "";
    }
    
    private string AbortMatch(params string[] args)
    {
        return "";
    }
    
    private string MovePlayerToTeam(params string[] args)
    {
        return "";
    }
    
    private string ChangeBeatmap(params string[] args)
    {
        return "";
    }
    
    private string ChangeMods(params string[] args)
    {
        return "";
    }
    
    private string StartMatch(params string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (_lobby.BeatmapId < 0)
            return "No beatmap is selected.";
        
        if (_lobby.InProgress)
            return "Match is already in progress.";
        
        var seconds = args.Length == 0 ? 30 : uint.Parse(args[0]);
        
        //TODO should this overwrite current timer or display this message?
        if (_lobby.Timer != null)
            return "Timer is already running.";
        
        _lobby.Timer = new LobbyTimer(_lobby, seconds, true);
        _lobby.ReadyAllPlayers();
        
        return "";
    }
    
    private string StartTimer(params string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        var seconds = args.Length == 0 ? 30 : uint.Parse(args[0]);
        
        //TODO should this overwrite current timer or display this message?
        if (_lobby.Timer != null)
            return "Timer is already running.";
        
        _lobby.Timer = new LobbyTimer(_lobby, seconds);
        
        return "";
    }
    
    private string AbortTimer()
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (_lobby.Timer == null)
            return "No timer is running.";
        
        _lobby.Timer.Stop();
        
        return "Aborted current timer.";
    }
    
    private string KickPlayer(params string[] args)
    {
        return "";
    }
    
    private string BanPlayer(params string[] args)
    {
        return "";
    }
    
    private async Task<string> AddReferee(params string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        if (args.Length == 0)
            return "No referee(s) provided. Use 'mp addref <username> [<username>] ...'.";

        int count = 0;
        foreach (var username in args)
        {
            var player = await players.GetPlayerOrOffline(username);

            if (player == _playerCtx)
            {
                _lobby.Chat.SendBotMessage("You can't add yourself as referee.");
                continue;
            }
            if (player == null)
            {
                _lobby.Chat.SendBotMessage($"{username} doesn't exist");
                continue;
            }
            if (_lobby.Refs.Contains(player.Id))
            {
                _lobby.Chat.SendBotMessage($"Player {player.Username} is already referee.");
                continue;
            }
            _lobby.Refs.Add(player.Id);
            count++;
        }
        Console.WriteLine($"[AddReferee] {_playerCtx.Username} added {count} players as referee(s). in lobby with id {_lobby.Id}.");
        return $"Added {count} players as referee(s).";
    }
    
    private async Task<string> RemoveReferee(params string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        if (args.Length == 0)
            return "No referee(s) provided. Use 'mp rmref <username> [<username>] ...'.";

        int count = 0;
        foreach (var username in args)
        {
            var player = await players.GetPlayerOrOffline(username);
            
            if (player == _playerCtx)
            {
                _lobby.Chat.SendBotMessage("You can't remove yourself from referees.");
                continue;
            }
            if (player == null)
            {
                _lobby.Chat.SendBotMessage($"{username} doesn't exist");
                continue;
            }
            if (!_lobby.Refs.Contains(player.Id))
            {
                _lobby.Chat.SendBotMessage($"Player {player.Username} is not referee.");
                continue;
            }
            _lobby.Refs.Remove(player.Id);
            count++;
        }
        Console.WriteLine($"[AddReferee] {_playerCtx.Username} removed {count} players as referee(s). in lobby with id {_lobby.Id}.");
        return $"Removed {count} players as referee(s).";
    }
    
    private string ListReferees()
    {
        return $"{string.Join(", ", _lobby.Refs.Select(id => players.GetPlayerOrOffline(id).Result!.Username))}";
    }
    
    private string CloseLobby(params string[] args)
    {
        return "";
    }
}