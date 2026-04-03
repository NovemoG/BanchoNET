using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Scores;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Spectator;

[Serializable]
[MessagePackObject]
public class SpectatorState : IEquatable<SpectatorState>
{
    [Key(0)]
    public int? BeatmapID { get; set; }
    
    [Key(1)]
    public int? RulesetID { get; set; }
    
    [Key(2)]
    public IEnumerable<ApiMod> Mods { get; set; } = [];
    
    [Key(3)]
    public SpectatedUserState State { get; set; }

    [Key(4)]
    public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new();

    public bool Equals(
        SpectatorState? other
    ) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return BeatmapID == other.BeatmapID && Mods.SequenceEqual(other.Mods) && RulesetID == other.RulesetID && State == other.State;
    }
    
    public override bool Equals(object? obj) => Equals(obj as SpectatorState);
}