using BanchoNET.Core.Models.Db;

namespace BanchoNET.Core.Abstractions.Services;

public interface ISearchProjectionSyncService
{
    Task RefreshAsync(
        BanchoDbContext db,
        int[] beatmapIds,
        int[] setIds,
        CancellationToken ct
    );
}