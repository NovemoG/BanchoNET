using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Beatmaps;

namespace BanchoNET.Core.Abstractions.Services;

public interface IBeatmapHandler
{
    Task<bool> CheckIfMapExistsOnBanchoByFilename(string filename);
    Task<bool> EnsureLocalBeatmapFile(Beatmap beatmap);
    
    Task FetchPlayerPlaycount(
        ApiBeatmapsetFull beatmapset,
        int playerId
    );
    
    Task FetchPlayerFavorited(
        ApiBeatmapsetFull beatmapset,
        int playerId
    );
    
    Task<Beatmap?> GetBeatmap(int mapId, int setId = -1);
    Task<Beatmap?> GetBeatmap(string beatmapMD5, int setId = -1);
    Task<Beatmapset?> GetBeatmapset(int setId, int mapId = -1, bool recheckApi = false, CancellationToken cancellationToken = default);
    Task<List<Beatmap>> GetBeatmaps(int[] beatmapIds);

    Task<ApiBeatmapsetFull?> GetBeatmapsetFromApiOrCached(
        int beatmapsetId,
        bool withAllData = true
    );

    Task<ApiBeatmapsetFull?> GetBeatmapsetByMapIdFromApiOrCached(
        int mapId,
        bool withAllData = true
    );

    /// <summary>
    /// Fetches a beatmapset from the official server and stores it, without adding it to the in-memory cache.
    /// </summary>
    Task<BeatmapsetImportResult> ImportBeatmapsetFromApi(
        int beatmapsetId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns the id of the newest beatmapset on the official server, or null if it couldn't be determined.
    /// </summary>
    Task<int?> GetNewestBeatmapsetIdFromApi(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Unranks a set, or one of its beatmaps, by giving it back its status from the official server and refreshing the set.
    /// Sets the official server doesn't have become pending.
    /// </summary>
    Task<bool> RestoreOfficialStatus(
        int setId,
        int mapId = -1,
        CancellationToken cancellationToken = default
    );
}