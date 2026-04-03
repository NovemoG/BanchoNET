using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match;

[Serializable]
[MessagePackObject]
[Union(0, typeof(TeamVersusUserState))]
public class MatchUserState
{
    [MessagePackObject]
    public class TeamVersusUserState : MatchUserState
    {
        [Key(0)]
        public int TeamID { get; set; }
    }
}