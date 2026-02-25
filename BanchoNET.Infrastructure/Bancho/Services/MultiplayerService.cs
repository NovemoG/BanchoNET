using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Multiplayer;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Services;

public sealed class MultiplayerService(ILogger logger) : StatefulService<int, MultiplayerMatch>(logger), IMultiplayerService
{
    private uint _nextMatchId;
    public ushort GetFreeMatchId => (ushort)Interlocked.Increment(ref _nextMatchId);
    public IEnumerable<MultiplayerMatch> Matches => Items.Values;
    
    public bool InsertLobby(
        MultiplayerMatch match
    ) {
        var added = Items.TryAdd(match.Id, match);
        
        if (!added)
            Logger.LogWarning($"Failed to insert match with ids {match.Id}, {match.LobbyId}");

        return added;
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