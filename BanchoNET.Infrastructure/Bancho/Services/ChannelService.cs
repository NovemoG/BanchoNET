using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Services;

public class ChannelService(
    ILogger logger,
    IPlayerService players
) : StatefulService<string, Channel>(logger), IChannelService
{
    public IEnumerable<Channel> Channels => Items.Values;
    
    public Channel LobbyChannel
    {
        get
        {
            if (TryGet("#lobby", out var lobby))
                return lobby!;
			
            Logger.LogWarning("Couldn't find '#lobby' channel, creating a default one.");

            lobby = ChannelExtensions.DefaultChannels.First(c => c.IdName == "#lobby");
            TryAdd("#lobby", lobby);
            return lobby;
        }
    }
    
    public bool InsertChannel(
        Channel channel
    ) {
        var added = base.TryAdd(channel);
        
        if (!added)
            Logger.LogWarning($"Failed to add channel {channel.IdName}");
        
        return added;
    }

    public bool RemoveChannel(
        Channel channel
    ) {
        return TryRemove(channel.IdName);
    }

    public bool RemoveChannel(
        string id
    ) {
        return TryRemove(id);
    }

    public Channel? GetChannel(
        string name
    ) {
        return TryGet(name, out var channel) ? channel : null;
    }

    public bool JoinPlayer(
        Channel channel,
        User player
    ) {
        if (channel.PlayerInChannel(player) ||
            !channel.CanPlayerRead(player) ||
            (channel.IdName == "#lobby" && !player.InLobby))
        {
            Logger.LogWarning($"{player.Username} failed to join {channel.IdName}");
            return false;
        }

        channel.AddPlayer(player);
        player.Channels.Add(channel.IdName);
		
        player.Enqueue(new ServerPackets()
            .ChannelJoin(channel.Name)
            .FinalizeAndGetContent());
		
        var bytes = new ServerPackets()
            .ChannelInfo(channel)
            .FinalizeAndGetContent();
		
        if (channel.Instance)
            foreach (var user in channel.Players)
                user.Enqueue(bytes);
        else
            foreach (var user in players.Players.Where(channel.CanPlayerRead))
                user.Enqueue(bytes);
		
        Logger.LogDebug($"{player.Username} joined {channel.IdName}");
        return true;
    }

    public bool LeavePlayer(
        Channel channel,
        User player,
        bool kick = true
    ) {
        if (!channel.PlayerInChannel(player))
        {
            Logger.LogDebug($"{player.Username} tried to leave {channel.IdName} without being in it ");
            return false;
        }
		
        channel.RemovePlayer(player);
        player.Channels.Remove(channel.IdName);

        if (kick)
        {
            player.Enqueue(new ServerPackets()
                .ChannelKick(channel.Name)
                .FinalizeAndGetContent());
        }
		
        var bytes = new ServerPackets()
            .ChannelInfo(channel)
            .FinalizeAndGetContent();

        if (channel.Instance)
            foreach (var user in channel.Players)
                user.Enqueue(bytes);
        else
            foreach (var user in players.Players.Where(channel.CanPlayerRead))
                user.Enqueue(bytes);
		
        Logger.LogDebug($"{player.Username} left {channel.IdName}");
        return true;
    }

    public bool LeavePlayer(
        string id,
        User player,
        bool kick = true
    ) {
        if (TryGet(id, out var channel))
            return LeavePlayer(channel!, player, kick);
        
        Logger.LogDebug($"{player.Username} failed to leave channel {id}");
        return false;
    }

    public void SendMessageTo(
        Channel channel,
        Message message,
        bool toSelf = false
    ) {
        var messageBytes = new ServerPackets()
            .SendMessage(new Message
            {
                Sender = message.Sender,
                Content = message.Content,
                Destination = channel.Name,
                SenderId = message.SenderId
            })
            .FinalizeAndGetContent();
		
        foreach (var player in channel.Players)
        {
            if (!player.BlockedByPlayer(message.SenderId) && (toSelf || player.Id != message.SenderId))
                player.Enqueue(messageBytes);
        }
    }

    public void SendBotMessageTo(
        Channel channel,
        string message,
        User from
    ) {
        if (message.Length >= 31979)
            message = $"message would have crashed games ({message.Length} characters).";
		
        channel.EnqueueToPlayers(new ServerPackets()
            .SendMessage(new Message
            {
                Sender = from.Username,
                Content = message,
                Destination = channel.Name,
                SenderId = from.Id
            })
            .FinalizeAndGetContent());
    }
    
    protected bool TryAdd(
        string key,
        Channel channel
    ) {
        var added = base.TryAdd(channel);
        
        if (!added)
            Logger.LogWarning($"Failed to add channel {key}");
        
        return added;
    }

    protected bool TryRemove(
        string key
    ) {
        var removed = base.TryRemove(key, out _);
        
        if (!removed)
            Logger.LogWarning($"Failed to remove channel {key}");
        
        return removed;
    }
}