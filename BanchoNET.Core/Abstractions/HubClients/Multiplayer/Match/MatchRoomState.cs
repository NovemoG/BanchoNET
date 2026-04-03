using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.RankedPlay;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match;

[Serializable]
[MessagePackObject]
[Union(0, typeof(TeamVersusRoomState))]
[Union(1, typeof(MatchmakingRoomState))]
[Union(2, typeof(RankedPlayRoomState))]
public abstract class MatchRoomState
{
    [MessagePackObject]
    public class TeamVersusRoomState : MatchRoomState
    {
        [Key(0)]
        public List<MultiplayerTeam> Teams { get; set; } = [];

        [Key(1)]
        public bool Locked { get; set; }

        public static TeamVersusRoomState CreateDefault() =>
            new()
            {
                Teams =
                {
                    new MultiplayerTeam { ID = 0, Name = "Team Red" },
                    new MultiplayerTeam { ID = 1, Name = "Team Blue" },
                }
            };
    }
    
    [Serializable]
    [MessagePackObject]
    public class MatchmakingRoomState : MatchRoomState
    {
        [Key(0)]
        public MatchmakingStage Stage { get; set; }
        
        [Key(1)]
        public int CurrentRound { get; set; }
        
        [Key(2)]
        public long[] CandidateItems { get; set; } = [];
        
        [Key(3)]
        public long CandidateItem { get; set; }
        
        [Key(4)]
        public MatchmakingUserList Users { get; set; } = new();
        
        [Key(5)]
        public long GameplayItem { get; set; }
        
        public void AdvanceRound()
        {
            CurrentRound++;
        }
    }
    
    [Serializable]
    [MessagePackObject]
    public class RankedPlayRoomState : MatchRoomState
    {
        [Key(0)]
        public RankedPlayStage Stage { get; set; }
        
        [Key(1)]
        public int CurrentRound { get; set; }
        
        [Key(2)]
        public double DamageMultiplier { get; set; } = 1;
        
        [Key(3)]
        public Dictionary<int, RankedPlayUserInfo> Users { get; set; } = [];
        
        [Key(4)]
        public int? ActiveUserId { get; set; }
        
        [Key(5)]
        public double StarRating { get; set; }
        
        [Key(6)]
        public int? WinningUserId { get; set; }
        
        [IgnoreMember]
        public RankedPlayUserInfo? ActiveUser => ActiveUserId == null ? null : Users[ActiveUserId.Value];

        [IgnoreMember]
        public RankedPlayUserInfo? WinningUser => WinningUserId == null ? null : Users[WinningUserId.Value];
    }
}