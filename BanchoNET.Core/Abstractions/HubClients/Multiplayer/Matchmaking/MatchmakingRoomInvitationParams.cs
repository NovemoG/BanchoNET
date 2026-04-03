using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingRoomInvitationParams
{
    [Key(0)]
    public MatchmakingPoolType Type { get; set; }
}