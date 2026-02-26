using BanchoNET.Core.Abstractions;

namespace BanchoNET.Core.Models.Beatmaps;

public interface IBeatmapSet : IHasOnlineId<int>,
    IEquatable<IBeatmapSet>
{
    int Id { get; set; }
    List<Beatmap> Beatmaps { get; }
    
    bool IEquatable<IBeatmapSet>.Equals(IBeatmapSet? other)
    {
        if (other == null)
            return false;

        return OnlineId == other.OnlineId && Beatmaps.SequenceEqual(other.Beatmaps);
    }
}