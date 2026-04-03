using System.Net.WebSockets;
using System.Text;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers;

[ApiController]
[Route("notify")]
[Authorize]
[SubdomainAuthorize("notify")]
public class NotifyController : ControllerBase
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
                using var ms = new MemoryStream();
                WebSocketReceiveResult? result;
                
                do
                {
                    result = await socket.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                        {
                            await CloseSocketAsync(socket,
                                WebSocketCloseStatus.NormalClosure,
                                "Closing"
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
                await CloseSocketAsync(socket,
                    WebSocketCloseStatus.NormalClosure,
                    "Server shutting down"
                );
            }
        }
    }
    
    private static async Task CloseSocketAsync(
        WebSocket socket,
        WebSocketCloseStatus status,
        string description
    ) {
        try
        {
            await socket.CloseAsync(status, description, CancellationToken.None);
        }
        catch { /* ignore shutdown errors */ }
    }
}