using System.Net.WebSockets;
using System.Text.Json;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Notify;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers;

[ApiController]
[Route("notify")]
[Authorize]
[SubdomainAuthorize("notify")]
public class NotifyController(ILogger logger, INotifySocketManager sockets) : ControllerBase
{
    [HttpGet]
    public async Task Get() {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("WebSocket requests only");
            return;
        }
        
        if (!HttpContext.User.TryGetUserId(out var uid))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsync("Unauthorized");
            return;
        }
        
        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var ct = HttpContext.RequestAborted;
        var buffer = new byte[1024];
        
        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var json = await socket.ReceiveTextAsync(buffer, ct);
                if (json is null)
                    break;
                
                if (string.IsNullOrWhiteSpace(json))
                    continue;

                var message = JsonSerializer.Deserialize<SocketMessage>(json, SnakeCaseNamingPolicy.Options);
                if (message is null)
                    continue;

                switch (message.Event)
                {
                    case "chat.start":
                        // the client wants to receive chat messages
                        sockets.Add(uid, socket);
                        break;
                    
                    case "chat.end":
                        // the client no longer wants to receive chat messages
                        sockets.Remove(uid);
                        break;
                    
                    default:
                        logger.LogDebug($"Unknown event ({message.Event}) received in a notification websocket for player: {uid}", caller: nameof(NotifyController));
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { /* graceful */ }
        catch (WebSocketException) { /* connection aborted */ }
        finally
        {
            sockets.Remove(uid);
            
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server shutting down",
                        CancellationToken.None
                    );
                }
                catch { /* ignore shutdown errors */ }
            }
        }
    }
}