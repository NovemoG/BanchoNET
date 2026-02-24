using BanchoNET.Core.Models.Multiplayer;

namespace BanchoNET.Core.Models.Dtos;

public class ScoreRequestDto
{
    public required List<int> Slots { get; init; }
    public required DateTime MapFinishDate { get; init; }
    public required MultiplayerMatch Match { get; init; }
}