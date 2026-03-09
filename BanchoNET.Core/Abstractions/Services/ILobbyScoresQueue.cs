using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Abstractions.Services;

public interface ILobbyScoresQueue
{
    Task EnqueueJobAsync(MatchScoreRequestDto request);
    Task<MatchScoreRequestDto> ReadJobAsync(CancellationToken ct);
}