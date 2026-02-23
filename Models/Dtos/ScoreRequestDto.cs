using BanchoNET.Objects.Multiplayer;

namespace BanchoNET.Models.Dtos;

public class ScoreRequestDto
{
    public required List<int> Slots { get; init; }
    public required DateTime MapFinishDate { get; init; }
    public required MultiplayerLobby Lobby { get; init; }
}