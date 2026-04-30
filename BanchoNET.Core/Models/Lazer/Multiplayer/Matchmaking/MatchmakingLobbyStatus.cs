using MessagePack;

namespace BanchoNET.Core.Models.Lazer.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingLobbyStatus
{
    [Key(0)]
    public int[] UsersInQueue { get; set; } = [];
}