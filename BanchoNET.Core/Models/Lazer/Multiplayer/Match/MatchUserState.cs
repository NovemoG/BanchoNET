using MessagePack;

namespace BanchoNET.Core.Models.Lazer.Multiplayer.Match;

[Serializable]
[MessagePackObject]
[Union(0, typeof(TeamVersusUserState))]
public abstract class MatchUserState
{
    [MessagePackObject]
    public class TeamVersusUserState : MatchUserState
    {
        [Key(0)]
        public int TeamID { get; set; }
    }
}