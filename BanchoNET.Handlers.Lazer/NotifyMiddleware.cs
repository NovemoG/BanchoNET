using System.Net.WebSockets;
using System.Text;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Http;

namespace BanchoNET.Handlers.Lazer;

public class NotifyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/notify"))
        {
            await next(context);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket requests only");
            return;
        }

        if (!context.User.TryGetUserId(out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
        
        var ct = context.RequestAborted;
        
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[8192];
        
        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult? result;
                
                do
                {
                    result = await socket.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                        {
                            await socket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Closing",
                                CancellationToken.None
                            );
                        }
                        return;
                    }
                    
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);
                
                ms.Position = 0;
                using var reader = new StreamReader(ms, Encoding.UTF8);
                var json = await reader.ReadToEndAsync(ct);
                
                if (string.IsNullOrWhiteSpace(json))
                    continue;
                
                //TODO handle notify
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { /* graceful */ }
        catch (WebSocketException) { /* connection aborted */ }
        finally
        {
            if (socket.State == WebSocketState.Open)
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