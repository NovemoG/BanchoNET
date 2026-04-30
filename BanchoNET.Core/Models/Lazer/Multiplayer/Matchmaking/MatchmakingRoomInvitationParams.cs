using MessagePack;

namespace BanchoNET.Core.Models.Lazer.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingRoomInvitationParams
{
    [Key(0)]
    public MatchmakingPoolType Type { get; set; }
}