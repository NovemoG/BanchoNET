using System.Collections.Concurrent;
using BanchoNET.Core.Models.Players;
using Novelog.Abstractions;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public abstract class PlayerStateService(ILogger logger) : StatefulService<int, Player>(logger)
{
    //PlayersById already exist in StatefulService
    protected readonly ConcurrentDictionary<string, Player> PlayersByUsername = new();
    protected readonly ConcurrentDictionary<Guid, Player> PlayersByToken = new();
    
    protected readonly ConcurrentDictionary<Player, bool> PlayersInLobby = new();
    
    protected readonly ConcurrentDictionary<int, Player> RestrictedById = new();
    protected readonly ConcurrentDictionary<string, Player> RestrictedByUsername = new();
    protected readonly ConcurrentDictionary<Guid, Player> RestrictedByToken = new();
    
    protected readonly ConcurrentDictionary<int, Player> BotsById = new();
    protected readonly ConcurrentDictionary<string, Player> BotsByUsername = new();

    protected override bool TryAdd(
        Player value
    ) {
        if (value.IsRestricted)
        {
            return RestrictedByToken.TryAdd(value.SessionId, value)
                   && RestrictedByUsername.TryAdd(value.SafeName, value)
                   && RestrictedById.TryAdd(value.Id, value);
        }

        return PlayersByToken.TryAdd(value.SessionId, value)
               && PlayersByUsername.TryAdd(value.SafeName, value)
               && base.TryAdd(value);
    }

    protected bool TryAddBot(
        Player value
    ) {
        value.IsBot = true;
        
        return BotsById.TryAdd(value.Id, value)
               && BotsByUsername.TryAdd(value.SafeName, value);
    }

    protected bool TryRemove(
        Player value
    ) {
        if (PlayersByUsername.TryRemove(value.SafeName, out _)
            && PlayersByToken.TryRemove(value.SessionId, out _)
            && base.TryRemove(value.Id, out _))
        {
            if (value.InLobby)
                PlayersInLobby.TryRemove(value, out _);
            
            Logger.LogDebug($"Removed player {value.Username}");
            return true;
        }
        
        if (RestrictedById.TryRemove(value.Id, out _)
            && RestrictedByUsername.TryRemove(value.SafeName, out _)
            && RestrictedByToken.TryRemove(value.SessionId, out _))
        {
            Logger.LogDebug($"Removed restricted player {value.Username}");
            return true;
        }

        if (BotsById.TryRemove(value.Id, out _)
            && BotsByUsername.TryRemove(value.SafeName, out _))
        {
            Logger.LogDebug($"Removed bot {value.Username}");
            return true;
        }

        Logger.LogWarning($"Failed to remove player {value.Username}");
        return false;
    }
}