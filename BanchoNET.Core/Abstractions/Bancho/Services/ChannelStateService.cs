using BanchoNET.Core.Models.Channels;
using Novelog.Abstractions;

namespace BanchoNET.Core.Abstractions.Bancho.Services;

public class ChannelStateService(ILogger logger) : StatefulService<string, Channel>(logger)
{
    protected bool TryAdd(
        string key,
        Channel channel
    ) {
        var added = base.TryAdd(channel);
        
        if (!added)
            Logger.LogWarning($"Failed to add channel {key}");
        
        return added;
    }

    protected bool TryRemove(
        string key
    ) {
        var removed = base.TryRemove(key, out _);
        
        if (!removed)
            Logger.LogWarning($"Failed to remove channel {key}");
        
        return removed;
    }
}