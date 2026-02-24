using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Users;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Services;

public sealed class PlayerService(ILogger logger) : StatefulService<int, User>(logger), IPlayerService
{
    
    
    public void Dispose() {
        foreach (var player in _items.Values)
        {
            player.Dispose();
        }
    }
}