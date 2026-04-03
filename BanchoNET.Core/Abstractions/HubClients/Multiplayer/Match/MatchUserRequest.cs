using System.Text.Json.Serialization;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match;

[Serializable]
[MessagePackObject]
[Union(0, typeof(ChangeTeamRequest))]
[Union(1, typeof(StartMatchCountdownRequest))]
[Union(2, typeof(StopCountdownRequest))]
[Union(3, typeof(MatchmakingAvatarActionRequest))]
[Union(4, typeof(RankedPlayCardHandReplayRequest))]
[Union(5, typeof(SetLockStateRequest))]
[Union(6, typeof(RollRequest))]
public abstract class MatchUserRequest
{
    [MessagePackObject]
    public class ChangeTeamRequest : MatchUserRequest
    {
        [Key(0)]
        public int TeamID { get; set; }
    }
    
    [MessagePackObject]
    public class StartMatchCountdownRequest : MatchUserRequest
    {
        [Key(0)]
        public TimeSpan Duration { get; set; }
    }
    
    [MessagePackObject]
    public class StopCountdownRequest : MatchUserRequest
    {
        [Key(0)]
        public readonly int ID;

        [JsonConstructor]
        [SerializationConstructor]
        public StopCountdownRequest(int id)
        {
            ID = id;
        }
    }
    
    [Serializable]
    [MessagePackObject]
    public class MatchmakingAvatarActionRequest : MatchUserRequest
    {
        [Key(0)]
        public MatchmakingAvatarAction Action { get; set; }
    }
    
    [Serializable]
    [MessagePackObject]
    public class RankedPlayCardHandReplayRequest : MatchUserRequest
    {
        [Key(0)]
        public required RankedPlayCardHandReplayFrame[] Frames { get; init; }
    }
    
    [MessagePackObject]
    public class SetLockStateRequest : MatchUserRequest
    {
        [Key(0)]
        public bool Locked { get; set; }
    }
    
    [Serializable]
    [MessagePackObject]
    public class RollRequest : MatchUserRequest
    {
        [Key(0)]
        public uint? Max { get; set; }
    }
}