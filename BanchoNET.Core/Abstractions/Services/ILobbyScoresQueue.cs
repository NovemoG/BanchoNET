using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Abstractions.Services;

public interface ILobbyScoresQueue
{
    Task EnqueueJobAsync(ScoreRequestDto request);
    Task<ScoreRequestDto> ReadJobAsync(CancellationToken ct);
}