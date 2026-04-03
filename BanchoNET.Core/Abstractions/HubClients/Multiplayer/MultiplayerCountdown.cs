using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer;

[MessagePackObject]
[Union(0, typeof(MatchStartCountdown))]
[Union(1, typeof(ForceGameplayStartCountdown))]
[Union(2, typeof(ServerShuttingDownCountdown))]
[Union(3, typeof(MatchmakingStageCountdown))]
[Union(4, typeof(RankedPlayStageCountdown))]
public abstract class MultiplayerCountdown
{
    [Key(0)]
    public int ID { get; set; }
    
    [Key(1)]
    public TimeSpan TimeRemaining { get; set; }
    
    [IgnoreMember]
    public virtual bool IsExclusive => true;
    
    [MessagePackObject]
    public sealed class MatchStartCountdown : MultiplayerCountdown { }
    
    [MessagePackObject]
    public sealed class ForceGameplayStartCountdown : MultiplayerCountdown { }
    
    [MessagePackObject]
    public class ServerShuttingDownCountdown : MultiplayerCountdown { }
    
    [MessagePackObject]
    public class MatchmakingStageCountdown : MultiplayerCountdown
    {
        [Key(2)]
        public MatchmakingStage Stage { get; set; }
    }
    
    [MessagePackObject]
    public class RankedPlayStageCountdown : MultiplayerCountdown
    {
        [Key(2)]
        public RankedPlayStage Stage { get; set; }
    }
}