using System.Text.Json.Serialization;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match;

[Serializable]
[MessagePackObject]
[Union(0, typeof(CountdownStartedEvent))]
[Union(1, typeof(CountdownStoppedEvent))]
[Union(2, typeof(MatchmakingAvatarActionEvent))]
[Union(3, typeof(RankedPlayCardHandReplayEvent))]
[Union(4, typeof(RollEvent))]
public abstract class MatchServerEvent
{
    [MessagePackObject]
    public class CountdownStartedEvent : MatchServerEvent
    {
        [Key(0)]
        public readonly MultiplayerCountdown Countdown;

        [JsonConstructor]
        [SerializationConstructor]
        public CountdownStartedEvent(MultiplayerCountdown countdown)
        {
            Countdown = countdown;
        }
    }
    
    [MessagePackObject]
    public class CountdownStoppedEvent : MatchServerEvent
    {
        [Key(0)]
        public readonly int ID;

        [JsonConstructor]
        [SerializationConstructor]
        public CountdownStoppedEvent(int id)
        {
            ID = id;
        }
    }
    
    [Serializable]
    [MessagePackObject]
    public class MatchmakingAvatarActionEvent : MatchServerEvent
    {
        [Key(0)]
        public int UserId { get; set; }
        
        [Key(1)]
        public MatchmakingAvatarAction Action { get; set; }
    }
    
    [Serializable]
    [MessagePackObject]
    public class RankedPlayCardHandReplayEvent : MatchServerEvent
    {
        [Key(0)]
        public int UserId { get; set; }

        [Key(1)]
        public required RankedPlayCardHandReplayFrame[] Frames { get; init; }
    }
    
    [Serializable]
    [MessagePackObject]
    public class RollEvent : MatchServerEvent
    {
        [Key(0)]
        public int UserID { get; set; }
        
        [Key(1)]
        public uint Max { get; set; }
        
        [Key(2)]
        public uint Result { get; set; }
    }
}