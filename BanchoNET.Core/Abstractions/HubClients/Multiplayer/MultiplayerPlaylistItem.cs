using BanchoNET.Core.Models.Api;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer;

[Serializable]
[MessagePackObject]
public class MultiplayerPlaylistItem
{
    [Key(0)]
    public long ID { get; set; }

    [Key(1)]
    public int OwnerID { get; set; }

    [Key(2)]
    public int BeatmapID { get; set; }

    [Key(3)]
    public string BeatmapChecksum { get; set; } = string.Empty;

    [Key(4)]
    public int RulesetID { get; set; }
    
    [Key(5)]
    public IEnumerable<ApiMod> RequiredMods { get; set; } = [];
    
    [Key(6)]
    public IEnumerable<ApiMod> AllowedMods { get; set; } = [];
    
    [Key(7)]
    public bool Expired { get; set; }
    
    [Key(8)]
    public ushort PlaylistOrder { get; set; }
    
    [Key(9)]
    public DateTimeOffset? PlayedAt { get; set; }

    [Key(10)]
    public double StarRating { get; set; }
    
    [Key(11)]
    public bool Freestyle { get; set; }
    
    [SerializationConstructor]
    public MultiplayerPlaylistItem() { }
}