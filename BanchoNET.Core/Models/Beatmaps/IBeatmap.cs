using BanchoNET.Core.Abstractions;

namespace BanchoNET.Core.Models.Beatmaps;

public interface IBeatmap : IHasOnlineId<int>,
    IEquatable<IBeatmap>
{
    int Id { get; set; }
    int SetId { get; set; }
    
    string MD5 { get; set; }
    
    //TODO
    
    bool IEquatable<IBeatmap>.Equals(IBeatmap? other)
    {
        if (other == null)
            return false;

        return OnlineId == other.OnlineId && SetId == other.SetId && MD5 == other.MD5;
    }
}