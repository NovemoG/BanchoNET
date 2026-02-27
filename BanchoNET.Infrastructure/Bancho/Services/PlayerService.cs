using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
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
    
    

    /// <summary>
    /// Sends a message directly to this player. If channel is provided, it will be sent to that channel instead.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="message">Message content</param>
    /// <param name="from">Player that sent message</param>
    /// <param name="toChannel">(optional) channel that this message should be sent to instead of player</param>
    public void SendMessageTo(
        User player,
        string message,
        User from,
        Channel? toChannel = null
    ) {
        player.Enqueue(new ServerPackets()
            .SendMessage(new Message
            {
                Sender = from.Username,
                Content = message,
                Destination = toChannel != null ? toChannel.Name : player.Username,
                SenderId = from.Id
            }).FinalizeAndGetContent());
    }

    /// <summary>
    /// Sends a bot message to a specified player. If a channel name is provided, the message will be sent to that channel instead.
    /// </summary>
    /// <param name="player">The player who will receive the message.</param>
    /// <param name="message">The content of the message to be sent.</param>
    /// <param name="toChannel">(optional) The name of the channel to which the message should be sent.</param>
    public void SendBotMessageTo(
        User player,
        string message,
        string toChannel = ""
    ) {
        player.Enqueue(new ServerPackets()
            .SendMessage(new Message
            {
                Sender = BanchoBot.Username,
                Content = message,
                Destination = string.IsNullOrEmpty(toChannel) ? player.Username : toChannel,
                SenderId = BanchoBot.Id
            }).FinalizeAndGetContent());
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