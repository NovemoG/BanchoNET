using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Stable.Multiplayer;

namespace BanchoNET.Infrastructure.Bancho.Services;

public sealed class MultiplayerService(ILogger logger) : StatefulService<long, MultiplayerMatch>(logger), IMultiplayerService
{
    private uint _nextMatchId;
    
    public IEnumerable<MultiplayerMatch> Matches => Items.Values;
    
    public ushort InsertLobby(
        MultiplayerMatch match
    ) {
        while (true)
        {
            var id = (ushort)Interlocked.Increment(ref _nextMatchId);

            if (Items.TryAdd(id, match))
            {
                match.Id = id;
                return id;
            }
            
            Logger.LogWarning($"Failed to insert match with ids {id}, {match.LobbyId}");
        }
    }
    
    public bool RemoveLobby(MultiplayerMatch match)
    {
        var removed = TryRemove(match.Id, out _);
        
        if (!removed)
            Logger.LogWarning($"Failed to remove match with ids {match.Id}, {match.LobbyId}");

        return removed;
    }

    public MultiplayerMatch? GetMatch(
        ushort id
    ) {
        return TryGet(id, out var match) ? match : null;
    }
}