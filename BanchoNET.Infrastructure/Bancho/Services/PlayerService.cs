using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils.Extensions;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Services;

public sealed class PlayerService(ILogger logger) : PlayerStateService(logger), IPlayerService
{
    public User BanchoBot => BotsById[2];
    public IEnumerable<User> Players => Items.Values;
    public new IEnumerable<User> PlayersInLobby => base.PlayersInLobby.Keys;
    public IEnumerable<User> Restricted => RestrictedById.Values;
    public IEnumerable<User> Bots => BotsById.Values;

    public bool InsertPlayer(
        User player,
        bool isBot = false
    ) {
        return isBot ? TryAddBot(player) : TryAdd(player);
    }

    public bool RemovePlayer(
        User player
    ) {
        return TryRemove(player);
    }
    
    public User? GetPlayer(int id)
    {
        if (id < 2) return null;

        Items.TryGetValue(id, out var sessionPlayer);
        if (sessionPlayer != null) return sessionPlayer;
		
        BotsById.TryGetValue(id, out sessionPlayer);
        if (sessionPlayer != null) return sessionPlayer;
		
        RestrictedById.TryGetValue(id, out sessionPlayer);
        return sessionPlayer;
    }

    public User? GetPlayer(string? username)
    {
        if (string.IsNullOrEmpty(username)) return null;

        username = username.MakeSafe();
		
        PlayersByUsername.TryGetValue(username, out var sessionPlayer);
        if (sessionPlayer != null) return sessionPlayer;
		
        BotsByUsername.TryGetValue(username, out sessionPlayer);
        if (sessionPlayer != null) return sessionPlayer;
		
        RestrictedByUsername.TryGetValue(username, out sessionPlayer);
        return sessionPlayer;
    }

    public User? GetPlayer(Guid token)
    {
        return PlayersByToken.TryGetValue(token, out var sessionPlayer)
            ? sessionPlayer
            : RestrictedByToken.TryGetValue(token, out sessionPlayer)
                ? sessionPlayer
                : null;
    }
    
    /// <summary>
    /// For player to see multiplayer matches use <see cref="IMultiplayerCoordinator.JoinLobby"/>
    /// </summary>
    public bool JoinLobby(User player)
    {
        if (base.PlayersInLobby.TryAdd(player, false))
        {
            player.InLobby = true;
            Logger.LogDebug($"Player {player.Username} joined the lobby");
            return true;
        }
        
        Logger.LogWarning($"Failed to add player {player.Username} to the lobby");
        return false;
    }

    public bool LeaveLobby(User player)
    {
        if (base.PlayersInLobby.TryRemove(player, out _))
        {
            player.InLobby = false;
            Logger.LogDebug($"Player {player.Username} left the lobby");
            return true;
        }
        
        Logger.LogWarning($"Failed to remove player {player.Username} from the lobby");
        return false;
    }
    
    public void EnqueueToPlayers(byte[] data)
    {
        foreach (var player in Players)
            player.Enqueue(data);

        foreach (var player in Restricted)
            player.Enqueue(data);
    }
    
    public void Dispose() {
        foreach (var player in Items.Values)
        {
            player.Dispose();
        }
    }
}