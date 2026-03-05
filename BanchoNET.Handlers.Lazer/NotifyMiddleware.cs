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
        
        if (context.User.TryGetUserId(out var userId))
        {
            var ct = context.RequestAborted;
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            
            try
            {
                var buffer = new byte[8192];

                while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult? result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            //close
                            return;
                        }
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    var json = await reader.ReadToEndAsync(ct);
                    if (string.IsNullOrWhiteSpace(json)) continue;
                    
                    //TODO handle notify
                }
            }
            catch (OperationCanceledException) { /* graceful */ }
            catch (WebSocketException) { /* connection aborted */ }
            finally
            {
                //close
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
        }
    }
}