using System.Text.Json.Serialization;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer;

[MessagePackObject]
[method: JsonConstructor]
public class BeatmapAvailability(DownloadState state, float? downloadProgress = null)
{
    [Key(0)]
    public readonly DownloadState State = state;
    
    [Key(1)]
    public readonly float? DownloadProgress = downloadProgress;
    
    public static BeatmapAvailability Unknown() => new(DownloadState.Unknown);
    public static BeatmapAvailability NotDownloaded() => new(DownloadState.NotDownloaded);
    public static BeatmapAvailability Downloading(float progress) => new(DownloadState.Downloading, progress);
    public static BeatmapAvailability Importing() => new(DownloadState.Importing);
    public static BeatmapAvailability LocallyAvailable() => new(DownloadState.LocallyAvailable);

    public bool Equals(BeatmapAvailability? other) => other != null && State == other.State && DownloadProgress == other.DownloadProgress;

    public override string ToString() => $"{string.Join(", ", State, $"{DownloadProgress:0.00%}")}";
}