namespace BanchoNET.Core.Abstractions.Services;

public interface ISearchProjectionSyncService
{
    Task RefreshAsync(
        int[] beatmapIds,
        int[] setIds,
        CancellationToken ct
    );
}