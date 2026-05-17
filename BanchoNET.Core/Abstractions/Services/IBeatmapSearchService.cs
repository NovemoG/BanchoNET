using BanchoNET.Core.Models.Api.Beatmaps;

namespace BanchoNET.Core.Abstractions.Services;

public interface IBeatmapSearchService
{
    Task<(List<ApiBeatmapsetFull>, long, Dictionary<string, string>)> SearchAsync(
        string? q,
        string? mode,
        string? category,
        string? status,
        string? genre,
        string? language,
        string? extra,
        string? rankAchieved,
        string? sort,
        string? played,
        bool? nsfw,
        Dictionary<string, string>? cursor,
        CancellationToken ct = default
    );
}