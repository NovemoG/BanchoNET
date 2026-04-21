using System.Collections.Concurrent;
using System.Net.WebSockets;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Notify;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Infrastructure.Bancho.Services;

public sealed class NotifySocketManager : INotifySocketManager
{
    private readonly ConcurrentDictionary<int, WebSocket> _sockets = new();
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<int, bool>> _broadcasts = new();
    
    public void Add(int userId, WebSocket socket) => _sockets[userId] = socket;

    public void Remove(
        int userId
    ) {
        _sockets.TryRemove(userId, out _);

        foreach (var broadcast in _broadcasts.Values)
            broadcast.TryRemove(userId, out _);
    }

    public void JoinBroadcast(
        long channelId,
        int userId
    ) {
        if (_broadcasts.TryGetValue(channelId, out var broadcast))
            broadcast.TryAdd(userId, false);
        
        _broadcasts.TryAdd(channelId, new ConcurrentDictionary<int, bool>{ [userId] = false });
    }

    public void LeaveBroadcast(
        long channelId,
        int userId
    ) {
        if (_broadcasts.TryGetValue(channelId, out var broadcast))
            broadcast.TryRemove(userId, out _);
    }

    public async Task SendAsync(
        int userId,
        SocketMessage message,
        CancellationToken ct
    ) {
        if (!_sockets.TryGetValue(userId, out var socket))
            return;

        if (socket.State != WebSocketState.Open)
            return;

        await socket.SendJsonAsync(message, ct);
    }

    public async Task SendAsync<T>(
        int userId,
        string @event,
        T data,
        CancellationToken ct
    ) {
        await SendAsync(userId, new SocketMessage
        {
            Event = @event,
            Data = WebSocketExtensions.ToJsonElement(data)
        }, ct);
    }

    public async Task BroadcastToChannel(
        long channelId,
        ChannelMessage message,
        CancellationToken ct = default
    ) {
        var users = _broadcasts[channelId];
        
        foreach (var userId in users.Keys)
        {
            await SendAsync(userId, "chat.message.new", new NewChatMessageData
            {
                Messages = [message],
                Users = [message.Sender]
            }, CancellationToken.None);
        }
    }
}