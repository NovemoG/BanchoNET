using BanchoNET.Core.Abstractions;

namespace BanchoNET.Core.Models.Beatmaps;

public interface IBeatmap : IHasOnlineId<int>,
    IEquatable<IBeatmap>
{
    int Id { get; set; }
    int BeatmapsetId { get; set; }
    
    string Checksum { get; set; }
    
    //TODO
    
    bool IEquatable<IBeatmap>.Equals(IBeatmap? other)
    {
        if (other == null)
            return false;

        return OnlineId == other.OnlineId && BeatmapsetId == other.BeatmapsetId && Checksum == other.Checksum;
    }
}