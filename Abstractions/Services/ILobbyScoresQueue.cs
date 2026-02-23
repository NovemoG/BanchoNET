using BanchoNET.Models.Dtos;

namespace BanchoNET.Abstractions.Services;

public interface ILobbyScoresQueue
{
    Task EnqueueJobAsync(ScoreRequestDto request);
    Task<ScoreRequestDto> ReadJobAsync(CancellationToken ct);
}