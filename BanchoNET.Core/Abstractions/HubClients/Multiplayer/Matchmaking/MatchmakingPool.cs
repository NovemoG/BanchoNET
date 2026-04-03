using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

[Serializable]
[MessagePackObject]
public class MatchmakingPool
{
    [Key(0)]
    public int Id { get; set; }

    [Key(1)]
    public int RulesetId { get; set; }

    [Key(2)]
    public int Variant { get; set; }

    [Key(3)]
    public string Name { get; set; } = string.Empty;

    [Key(4)]
    public MatchmakingPoolType Type { get; set; } = MatchmakingPoolType.QuickPlay;
}