using System.Net.WebSockets;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Notify;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public interface INotifySocketManager
{
    void Add(
        int userId,
        WebSocket socket
    );
    
    void Remove(int userId);

    void JoinBroadcast(long channelId, int userId);
    void LeaveBroadcast(long channelId, int userId);

    Task SendAsync(
        int userId,
        SocketMessage message,
        CancellationToken ct
    );

    Task SendAsync<T>(
        int userId,
        string @event,
        T data,
        CancellationToken ct
    );

    Task BroadcastToChannel(
        long channelId,
        ChannelMessage message,
        CancellationToken ct = default
    );

    Task BroadcastPmMessage(
        ChannelMessage message,
        int senderId,
        int receiverId
    );
}