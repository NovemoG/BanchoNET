namespace BanchoNET.Core.Abstractions.Services.Lazer;

public interface IBeatmapChangeNotifier
{
    /// <summary>
    /// Tells connected lazer clients to refetch the given beatmap sets.
    /// </summary>
    Task BeatmapsetsChanged(
        int[] beatmapsetIds,
        int lastQueueId
    );
}