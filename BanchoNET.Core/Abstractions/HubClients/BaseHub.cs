using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BanchoNET.Core.Abstractions.HubClients;

[Authorize]
public abstract class BaseHub<T>(ILogger logger) : Hub<T> where T : class
{
    protected readonly ILogger Logger = logger;

    public override Task OnConnectedAsync() {
        if (!TryGetUserId(out var userId))
        {
            Logger.LogWarning($"Rejected {typeof(T).Name} connection: token carries no user", caller: GetType().Name);
            Context.Abort();

            return Task.CompletedTask;
        }

        Logger.LogInfo($"{typeof(T).Name} connected for user: {userId}", GetType().Name);
        
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(
        Exception exception
    ) {
        if (TryGetUserId(out var userId))
            Logger.LogInfo($"{typeof(T).Name} disconnected for user: {userId}", GetType().Name);
        
        return base.OnDisconnectedAsync(exception);
    }
    
    protected bool TryGetUserId(out int userId) => Context.User.TryGetUserId(out userId);
}