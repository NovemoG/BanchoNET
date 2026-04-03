using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
[Union(0, typeof(Searching))]
[Union(1, typeof(MatchFound))]
[Union(2, typeof(JoiningMatch))]
public abstract class MatchmakingQueueStatus
{
    [Serializable]
    [MessagePackObject]
    public class Searching : MatchmakingQueueStatus
    {
    }

    [Serializable]
    [MessagePackObject]
    public class MatchFound : MatchmakingQueueStatus
    {
    }

    [Serializable]
    [MessagePackObject]
    public class JoiningMatch : MatchmakingQueueStatus
    {
    }
}