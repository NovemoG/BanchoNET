using BanchoNET.Attributes;
using BanchoNET.Models.Mongo;
using BanchoNET.Objects;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using Action = BanchoNET.Models.Mongo.Action;
using static BanchoNET.Utils.Maps.CommandHandlerMap;
using static BanchoNET.Utils.Extensions.ModsExtensions;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    private MultiplayerLobby _lobby = null!;
    
    [Command("mp",
        PlayerPrivileges.Verified,
        "A set of commands to manage your multiplayer lobby. List of commands available under 'mp help' or 'help mp' command.",
        "\nmp create [<name>]/[<password>] - Creates a multiplayer lobby with a given name." +
        "\nmp invite/i <username> - Invites a player with a given username to your current multiplayer lobby." +
        "\nmp name <name> - Changes the name of your current multiplayer lobby." +
        "\nmp password/p [<password>] - Changes the password of your current multiplayer lobby. If <password> is not " +
        "provided it will be removed." +
        "\nmp lock - Locks the lobby so that players can’t change their team and slot." +
        "\nmp unlock - Reverses the above." +
        "\nmp size <size> - Sets the amount of available slots (1-16) in the lobby." +
        "\nmp set <teammode> [<scoremode>] [<size>] - Sets various lobby properties." +
        "\n        0: Head To Head, 1: Tag Coop, 2: Team Vs, 3: Tag Team Vs"+
        "\n        0: Score, 1: Accuracy, 2: Combo, 3: Score V2" +
        "\nmp move <username> <slot> - Moves a player within the lobby to the specified 1-indexed slot." +
        "\nmp host <username> - Transfers host to the player." +
        "\nmp clearhost/ch - Clears the lobby host." +
        "\nmp abort - Aborts the match." +
        "\nmp team <username> <color> - Moves a player to the specified team." +
        "\nmp map <mapid> [<gamemode>] - Changes the beatmap and gamemode of the lobby." +
        "\n        gamemode - 0: osu!, 1: Taiko, 2: Catch The Beat, 3: osu!Mania" +
        "\nmp mods <mod> [<mod>] ... - Removes all currently applied mods and applies these mods to the lobby." +
        "\nmp start [<seconds>] - Starts the match after a set time (in seconds) or instantaneously (if used by host " +
        "or all players are ready) if time is not present." +
        "\nmp timer [<seconds>] - Begins a countdown timer. Default is 30s." +
        "\nmp aborttimer/at - Stops the current timer (both normal timers and match start timer)" +
        "\n        Timer announcements occur every minute, 30s, 15s, 10s, 5s and earlier." +
        "\nmp kick <username> - Kicks the player from the lobby" +
        "\nmp ban <username> - Bans the player from the lobby" +
        "\nmp addref <username> [<username>] ... - Adds a referee to the lobby." +
        "\nmp rmref <username> [<username>] ... - Removes a referee from the lobby." +
        "\nmp listrefs - Lists all referees in the lobby" +
        "\nmp close - Disbands current lobby." +
        "\nYou can use these commands only in multiplayer lobby. Parameters inside of [] are optional.")]
    private async Task<(bool, string)> Multiplayer(string[] args)
    {
        var prefix = AppSettings.CommandPrefix;
        
        if (args.Length == 0)
            return (true, $"No parameter(s) provided. Check available options using '{prefix}mp help' or '{prefix}help mp'.");

        if (_playerCtx.Lobby == null && args[0].ToLower() is not ("help" or "create"))
            return (true, "You're not in a multiplayer lobby. Use 'mp create [<name>] [<password>]' to create one.");

        if (_playerCtx.Lobby != null && _playerCtx.Lobby.Chat != _channelCtx)
            return (true, "");

        _lobby = _playerCtx.Lobby!;

        return args[0] switch
        {
            "help" => (true, await Help(["mp"])),
            "create" => (true, await CreateMultiplayerLobby(args[1..])),
            "invite" => (false, InviteToLobby(args[1..])),
            "i" => (false, InviteToLobby(args[1..])),
            "name" => (false, ChangeLobbyName(args[1..])),
            "password" => (false, ChangeLobbyPassword(args[1..])),
            "p" => (false, ChangeLobbyPassword(args[1..])),
            "lock" => (false, LockLobby()),
            "unlock" => (false, UnlockLobby()),
            "size" => (false, SetLobbySize(args[1..])),
            "set" => (false, SetLobbyProperties(args[1..])),
            "move" => (false, MovePlayer(args[1..])),
            "host" => (false, await TransferHost(args[1..])),
            "clearhost" => (false, await ClearHost()),
            "ch" => (false, await ClearHost()),
            "abort" => (false, await AbortMatch()),
            "team" => (false, MovePlayerToTeam(args[1..])),
            "map" => (false, await ChangeBeatmap(args[1..])),
            "mods" => (false, ChangeMods(args[1..])),
            "start" => (false, StartMatch(args[1..])),
            "timer" => (false, StartTimer(args[1..])),
            "aborttimer" => (false, AbortTimer()),
            "at" => (false, AbortTimer()),
            "kick" => (false, await KickPlayer(args[1..])),
            "ban" => (false, await BanPlayer(args[1..])),
            "addref" => (false, await AddReferee(args[1..])),
            "rmref" => (false, await RemoveReferee(args[1..])),
            "listrefs" => (false, ListReferees()),
            "close" => (false, await CloseLobby()),
            _ => (true, $"Invalid parameter provided. Check available options using '{prefix}mp help' or '{prefix}help mp'.")
        };
    }

    private async Task<string> CreateMultiplayerLobby(string[] args)
    {
        if (_playerCtx.Lobby != null)
            return "";
        
        var lobbyDetails = string.Join(' ', args).Split('/', 2);
        var beatmap = await beatmaps.GetBeatmap(_playerCtx.LastValidBeatmapId);
        
        var lobby = new MultiplayerLobby
        {
            Name = lobbyDetails.Length > 0 ? lobbyDetails[0] : $"{_playerCtx.Username}'s match",
            Password = lobbyDetails.Length == 2 ? lobbyDetails[1] : "",
            HostId = _playerCtx.Id,
            CreatorId = _playerCtx.Id,
            Freemods = true,
            BeatmapId = beatmap?.MapId ?? -1,
            BeatmapMD5 = beatmap?.MD5 ?? "",
            BeatmapName = beatmap?.FullName() ?? "",
            Seed = Random.Shared.Next(),
        };

        foreach (var slot in lobby.Slots)
            slot.Status = SlotStatus.Open;
        
        MultiplayerExtensions.CreateLobby(lobby, _playerCtx, await histories.GetMatchId());
        
        await histories.InsertMatchHistory(new MultiplayerMatch
        {
            MatchId = lobby.LobbyId,
            Name = lobby.Name,
            Actions = [],
            Scores = [],
        });
        
        await histories.AddMatchAction(
            lobby.LobbyId,
            new ActionEntry
            {
                Action = Action.MatchCreated,
                PlayerId = _playerCtx.Id,
                Date = DateTime.Now
            });
        
        return "";
    }
    
    private string InviteToLobby(string[] args)
    {
        if (args.Length == 0)
            return $"No username provided. Use '{Prefix}mp invite <username>'.";

        var target = session.GetPlayerByName(args[0]);
        if (target == null)
            return $"{args[0]} is either offline or you misspelled the username.";

        if (target == _playerCtx)
            return "Why would you want to invite yourself?";
        
        MultiplayerExtensions.InviteToLobby(_playerCtx, target);
        
        return $"Invite sent to {target.Username}.";
    }
    
    private string ChangeLobbyName(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
            return $"No name provided. Use '{Prefix}mp name <name>'.";
        
        _lobby.Name = string.Join(' ', args);
        _lobby.EnqueueState();
        
        Console.WriteLine($"[ChangeLobbyName] {_playerCtx.Username} changed the match name to {_lobby.Name} in lobby with id {_lobby.Id}.");
        return $"Match name changed to {_lobby.Name}";
    }
    
    private string ChangeLobbyPassword(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
        {
            _lobby.Password = "";
            _lobby.EnqueueState();
            return "Password removed.";
        }
        
        _lobby.Password = string.Join(' ', args);
        _lobby.EnqueueState();
        
        Console.WriteLine($"[ChangeLobbyPassword] {_playerCtx.Username} changed match password to {_lobby.Password} in lobby with id {_lobby.Id}.");
        return $"Match password changed to {_lobby.Password}.";
    }
    
    private string LockLobby()
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        _lobby.Locked = true;
        
        return "Locked the lobby.";
    }
    
    private string UnlockLobby()
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        _lobby.Locked = false;
        
        return "Unlocked the lobby.";
    }
    
    private string SetLobbySize(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
            return $"Not enough parameters provided. Use '{Prefix}mp size <size>'.";
        
        if (!int.TryParse(args[0], out var size)) 
            return "Invalid size provided. Available slots: 1-16.";
        
        if (size is < 1 or > 16)
            return "Invalid size provided. Available slots: 1-16.";
        
        for (var i = 0; i < size; i++)
        {
            var slot = _lobby.Slots[i];

            slot.Status = slot.Status != SlotStatus.Locked ? slot.Status : SlotStatus.Open;
        }

        for (var i = size; i < _lobby.Slots.Length; i++)
        {
            var slot = _lobby.Slots[i];
            
            if ((slot.Status & SlotStatus.PlayerInSlot) != 0)
                slot.Player!.LeaveMatchToLobby();
            
            slot.Reset();
            slot.Status = SlotStatus.Locked;
        }
        
        _lobby.EnqueueState();

        return $"Changed size to: {size}";
    }
    
    private string SetLobbyProperties(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
            return $"Not enough parameters provided. Use '{Prefix}mp set <teammode> [<scoremode>] [<size>]'.";

        if (!int.TryParse(args[0], out var teamMode)) 
            return "Invalid team mode provided. Available modes: 0, 1, 2, 3.";

        var scoreMode = (int)_lobby.WinCondition;
        if (args.Length > 1)
            if (!int.TryParse(args[1], out scoreMode)) 
                scoreMode = (int)_lobby.WinCondition;
        
        if (teamMode is < 0 or > 3)
            return "Invalid team mode provided. Available modes: 0, 1, 2, 3.";
        
        if (scoreMode is < 0 or > 3)
            return "Invalid score mode provided. Available modes: 0, 1, 2, 3.";

        _lobby.Type = (LobbyType)teamMode;
        _lobby.WinCondition = (WinCondition)scoreMode;
        
        if (args.Length > 2)
            SetLobbySize(args[2..]);
        
        _lobby.EnqueueState();
        return $"Changed lobby properties to: Team mode: {_lobby.Type}, Score mode: {_lobby.WinCondition}, Size: {_lobby.Slots.Count(s => s.Status != SlotStatus.Locked)}.";
    }
    
    private string MovePlayer(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length < 2)
            return $"Not enough parameters provided. Use '{Prefix}mp move <username> <slot>'.";
        
        var oldSlot = _lobby.GetPlayerSlot(username: args[0]);
        if (oldSlot == null)
            return $"{args[0]} is not in the lobby.";

        if (!int.TryParse(args[1], out var newSlotNumber))
            return "Invalid slot number provided.";

        newSlotNumber--;
        if (newSlotNumber < 0 || newSlotNumber >= _lobby.Slots.Length)
            return "Invalid slot number provided.";
        
        var newSlot = _lobby.Slots[newSlotNumber];

        if (newSlot.Player != null)
        {
            var temp = newSlot.Copy();
            
            newSlot.CopyStatusFrom(oldSlot);
            newSlot.Status = SlotStatus.NotReady;
            
            oldSlot.CopyStatusFrom(temp);
            oldSlot.Status = SlotStatus.NotReady;
        }
        else
        {
            newSlot.CopyStatusFrom(oldSlot);
            newSlot.Status = SlotStatus.NotReady;
            
            oldSlot.Player = null;
            oldSlot.Status = SlotStatus.Open;
        }
        
        _lobby.EnqueueState();
        
        return $"Moved {args[0]} to slot {args[1]}.";
    }
    
    private async Task<string> TransferHost(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";   
        
        if (args.Length == 0)
            return $"No username provided. Use '{Prefix}mp host <username>'.";
        
        var slot = _lobby.GetPlayerSlot(args[0]);
        if (slot == null)
            return $"{args[0]} is not in the lobby.";
        
        var target = slot.Player!;
        _lobby.HostId = target.Id;
        
        _lobby.EnqueueState();
        
        await histories.AddMatchAction(
            _lobby.LobbyId,
            new ActionEntry
            {
                Action = Action.HostChanged,
                PlayerId = target.Id,
                Date = DateTime.Now
            });
        
        return $"Changed host to {target.Username}.";
    }
    
    private async Task<string> ClearHost()
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        _lobby.HostId = -1;
        _lobby.EnqueueState();
        
        await histories.AddMatchAction(
            _lobby.LobbyId,
            new ActionEntry
            {
                Action = Action.HostChanged,
                PlayerId = 0,
                Date = DateTime.Now
            });
        
        return "Removed host.";
    }
    
    private async Task<string> AbortMatch()
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";

        if (!_lobby.InProgress)
            return "Match is not in progress.";
        
        _lobby.End();

        await histories.MapAborted(_lobby.LobbyId);
        
        return "Aborted the match.";
    }
    
    private string MovePlayerToTeam(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";

        if (_lobby.Type is not (LobbyType.TeamVS or LobbyType.TagTeamVS))
            return "This command is only available in team-based lobbies.";
        
        if (args.Length < 2)
            return $"Not enough parameters provided. Use '{Prefix}mp team <username> <team>'.";
        
        var slot = _lobby.GetPlayerSlot(username: args[0]);
        if (slot == null)
            return $"{args[0]} is not in the lobby.";

        List<string> availableTeams = ["blue", "red"];
        
        if (!availableTeams.Contains(args[1].ToLower()))
            return $"Invalid team provided. Available teams: {string.Join(", ", availableTeams)}";

        slot.Team = args[1].ToLower() switch
        {
            "blue" => LobbyTeams.Blue,
            "red" => LobbyTeams.Red,
            _ => LobbyTeams.Neutral
        };
        
        slot.Status = SlotStatus.NotReady;
        _lobby.EnqueueState(false);
        
        return $"Moved {slot.Player!.Username} to {slot.Team} team.";
    }
    
    private async Task<string> ChangeBeatmap(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
            return $"No beatmap ID provided. Use '{Prefix}mp map <mapid> [<gamemode>]'.";

        if (!int.TryParse(args[0], out var beatmapId))
            return "Beatmap not found.";
        
        var gameMode = args.Length > 1 && int.TryParse(args[1], out var mode)
            ? (GameMode)mode
            : GameMode.VanillaStd;
        
        var beatmap = await beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null)
            return "Beatmap not found.";
        
        _lobby.BeatmapId = beatmapId;
        _lobby.BeatmapMD5 = beatmap.MD5;
        _lobby.BeatmapName = beatmap.FullName();
        _lobby.Mode = gameMode;
        
        _lobby.EnqueueState();
        
        return "";
    }
    
    private string ChangeMods(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        var result = Mods.None;
        var freeMods = false;
        
        foreach (var modName in args)
        {
            if (ModsMap.TryGetValue(modName.ToLower(), out var modMap))
            {
                if (modMap == Mods.None)
                {
                    _lobby.Mods = Mods.None;
                    break;
                }
                
                if (_lobby.Mode != GameMode.VanillaMania)
                    if (modMap > Mods.Perfect)
                        continue;

                result |= modMap;
            }
            else if (Enum.TryParse(modName, true, out Mods modParse))
            {
                if (modParse == Mods.None)
                {
                    _lobby.Mods = Mods.None;
                    break;
                }

                if (_lobby.Mode != GameMode.VanillaMania)
                    if (modParse > Mods.Perfect)
                        continue;

                result |= modParse;
            }
            else _lobby.Chat.SendBotMessage($"Invalid mod: {modName}");
            
            if (FreemodAliases.Any(a => a.Equals(modName, StringComparison.CurrentCultureIgnoreCase)))
            {
                _lobby.Freemods = true;
                freeMods = true;
            }
        }
        
        if ((result & Mods.InvalidMods) != 0)
            result &= ~Mods.InvalidMods;
        
        _lobby.Mods = result;
        _lobby.EnqueueState();

        switch (result)
        {
            case Mods.None when freeMods:
                _lobby.Freemods = true;
                return "Removed all mods and enabled freemods.";
            case Mods.None:
                return "Removed all mods.";
            default:
                return $"Changed mods to: {result.ToString()}";
        }
    }
    
    private string StartMatch(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (_lobby.BeatmapId < 1)
            return "No beatmap is selected.";
        
        if (_lobby.InProgress)
            return "Match is already in progress.";
        
        var seconds = args.Length == 0 ? 10 : uint.TryParse(args[0], out var s) ? s : 10;
        
        if (_lobby.Timer != null)
        {
            _lobby.Timer.Stop();
            _lobby.Chat.SendBotMessage("Updating current timer.");
        }

        _lobby.ReadyAllPlayers();
        _lobby.EnqueueState(false);
        
        _lobby.Timer = new LobbyTimer(_lobby, seconds, true, async () =>
            await histories.MapStarted(
                _lobby.LobbyId,
                new ScoresEntry
                {
                    StartDate = DateTime.Now,
                    GameMode = (byte)_lobby.Mode,
                    WinCondition = (byte)_lobby.WinCondition,
                    LobbyType = (byte)_lobby.Type,
                    LobbyMods = _lobby.Freemods ? 0 : (int)_lobby.Mods,
                    BeatmapId = _lobby.BeatmapId,
                    BeatmapName = _lobby.BeatmapName,
                    Values = []
                }));
        
        return "";
    }
    
    private string StartTimer(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        var seconds = args.Length == 0 ? 10 : uint.TryParse(args[0], out var s) ? s : 10;

        if (seconds == 0)
            return "";
        
        if (_lobby.Timer != null)
        {
            _lobby.Timer.Stop();
            _lobby.Chat.SendBotMessage("Updating current timer.");
        }
        
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
    
    private async Task<string> KickPlayer(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
            return $"No username provided. Use '{Prefix}mp kick <username>'.";
        
        var slot = _lobby.GetPlayerSlot(args[0]);
        if (slot == null)
            return $"{args[0]} is not in the lobby.";
        
        var player = slot.Player!;
        
        player.LeaveMatchToLobby();
        player.SendBotMessage("You've been kicked from the lobby.");
        
        await histories.AddMatchAction(
            _lobby.LobbyId,
            new ActionEntry
            {
                Action = Action.Left,
                PlayerId = player.Id,
                Date = DateTime.Now
            });

        if (_lobby.IsEmpty())
        {
            await histories.AddMatchAction(
                _lobby.LobbyId,
                new ActionEntry
                {
                    Action = Action.MatchDisbanded,
                    PlayerId = player.Id,
                    Date = DateTime.Now
                });
        }
        
        return $"{slot.Player!.Username} has been kicked.";
    }
    
    private async Task<string> BanPlayer(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
            return $"No username provided. Use '{Prefix}mp ban <username>'.";
        
        var target = await players.GetPlayerOrOffline(username: args[0]);
        if (target == null)
            return $"Player {args[0]} does not exist.";
        
        if (_lobby.BannedPlayers.Contains(target.Id))
            return $"{target.Username} is already banned.";

        _lobby.Refs.Remove(target.Id);
        _lobby.BannedPlayers.Add(target.Id);

        var slot = _lobby.GetPlayerSlot(target);
        if (slot != null)
        {
            var player = slot.Player!;
            
            player.LeaveMatchToLobby();
            player.SendBotMessage("You've been banned from the lobby.");
        }
        
        return $"{target.Username} has been banned.";
    }
    
    private async Task<string> AddReferee(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
            return $"No referee(s) provided. Use '{Prefix}mp addref <username> [<username>] ...'.";

        int count = 0;
        foreach (var username in args)
        {
            var player = await players.GetPlayerOrOffline(username);

            if (player == _playerCtx)
            {
                _lobby.Chat.SendBotMessage("You're already a referee.");
                continue;
            }
            if (player == null)
            {
                _lobby.Chat.SendBotMessage($"Player {username} doesn't exist");
                continue;
            }
            if (_lobby.Refs.Contains(player.Id))
            {
                _lobby.Chat.SendBotMessage($"{player.Username} is already a referee.");
                continue;
            }
            _lobby.Refs.Add(player.Id);
            count++;
        }
        Console.WriteLine($"[AddReferee] {_playerCtx.Username} added {count} players as referee(s). in lobby with id {_lobby.Id}.");
        return $"Added {count} players as referee(s).";
    }
    
    private async Task<string> RemoveReferee(string[] args)
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";
        
        if (args.Length == 0)
            return $"No referee(s) provided. Use '{Prefix}mp rmref <username> [<username>] ...'.";

        var count = 0;
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
                _lobby.Chat.SendBotMessage($"Player {username} doesn't exist");
                continue;
            }
            if (!_lobby.Refs.Contains(player.Id))
            {
                _lobby.Chat.SendBotMessage($"{player.Username} is not a referee.");
                continue;
            }
            if (_lobby.CreatorId == player.Id)
            {
                _lobby.Chat.SendBotMessage("Can't remove creator of the lobby.");
                continue;
            }
            
            _lobby.Refs.Remove(player.Id);
            count++;
        }
        
        Console.WriteLine($"[AddReferee] {_playerCtx.Username} removed {count} players from referees. In lobby with id {_lobby.Id}.");
        return $"Removed {count} players from referees.";
    }
    
    private string ListReferees()
    {
        //TODO wtf? refactor this
        return $"{string.Join(", ", _lobby.Refs.Select(id => players.GetPlayerOrOffline(id).Result!.Username))}";
    }
    
    private async Task<string> CloseLobby()
    {
        if (!_lobby.Refs.Contains(_playerCtx.Id))
            return "";

        var actions = new List<ActionEntry>();
        
        foreach (var slot in _lobby.Slots)
        {
            if (slot.Player == null) continue;
            
            actions.Add(new ActionEntry
            {
                Action = Action.Left,
                PlayerId = slot.Player.Id,
                Date = DateTime.Now
            });
            
            slot.Player.LeaveMatchToLobby();
        }
        
        actions.Add(new ActionEntry
        {
            Action = Action.MatchDisbanded,
            PlayerId = _playerCtx.Id,
            Date = DateTime.Now
        });

        await histories.AddMatchActions(_lobby.LobbyId, actions);
        
        return "";
    }
}