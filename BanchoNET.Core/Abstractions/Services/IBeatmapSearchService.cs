using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Beatmaps;

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
        int? playerId = null,
        PlayedScope playedScope = PlayedScope.Player,
        int count = 50,
        int? skip = null,
        CancellationToken ct = default
    );
}