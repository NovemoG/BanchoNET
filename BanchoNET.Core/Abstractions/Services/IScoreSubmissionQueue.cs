using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Scores;

namespace BanchoNET.Core.Abstractions.Services;

public interface IScoreSubmissionQueue
{
    Task<ScoreResponseDto?> EnqueueScore(
        ScoreRequestDto request,
        int userId,
        int beatmapId
    );

    Task<ApiScore?> SubmitScore(
        ScoreSubmitRequestDto request,
        long queueId,
        int userId,
        int beatmapId
    );
}