using BanchoNET.Core.Models.Stable.Multiplayer;

namespace BanchoNET.Core.Models.Dtos;

public class MatchScoreRequestDto
{
    public required List<int> Slots { get; init; }
    public required DateTime MapFinishDate { get; init; }
    public required MultiplayerMatch Match { get; init; }
}