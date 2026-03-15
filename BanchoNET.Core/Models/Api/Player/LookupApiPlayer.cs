using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Api.Player;

public class LookupApiPlayer : BasicApiPlayer
{
    public GlobalRank GlobalRank { get; set; } = new();
    public Group[] Groups { get; set; } = [];
    
    [JsonConstructor]
    public LookupApiPlayer() { }

    public LookupApiPlayer(
        Players.Player player,
        GlobalRank rank
    ) : base(player) {
        GlobalRank = rank;
    }

    public LookupApiPlayer(
        PlayerDto dto,
        GlobalRank rank
    ) : base(dto) {
        GlobalRank = rank;
    }
}

public class GlobalRank
{
    public int? Rank { get; set; }
    public int RulesetId { get; set; }
}