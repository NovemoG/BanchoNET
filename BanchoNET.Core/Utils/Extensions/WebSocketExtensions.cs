using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BanchoNET.Core.Models.Notify;
using BanchoNET.Core.Utils.Json;

namespace BanchoNET.Core.Utils.Extensions;

public static class WebSocketExtensions
{
    public static async Task<string?> ReceiveTextAsync(
        this WebSocket socket,
        byte[] buffer,
        CancellationToken ct
    ) {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);
        
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public static Task SendJsonAsync(
        this WebSocket socket,
        object payload,
        CancellationToken ct
    ) {
        var json = JsonSerializer.Serialize(payload, SnakeCaseNamingPolicy.Options);
        var bytes = Encoding.UTF8.GetBytes(json);

        return socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
    }

    public static JsonElement ToJsonElement<T>(T value)
        => JsonSerializer.SerializeToElement(value, SnakeCaseNamingPolicy.Options);

    public static T? DeserializeData<T>(
        this SocketMessage message
    ) {
        if (message.Data is null)
            return default;
        
        return message.Data.Value.Deserialize<T>(SnakeCaseNamingPolicy.Options);
    }
}