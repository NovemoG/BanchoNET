using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingLobbyStatus
{
    [Key(0)]
    public int[] UsersInQueue { get; set; } = [];
}