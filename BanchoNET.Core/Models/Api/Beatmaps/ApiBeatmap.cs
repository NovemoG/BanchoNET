using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ApiBeatmap : BasicApiBeatmap
{
    public int CurrentUserPlaycount { get; set; }
    public string[] CurrentUserTagIds { get; set; } = [];
    public Failtime Failtimes { get; set; }
    public List<Owner>? Owners { get; set; }
    public List<MapTag> TopTagIds { get; set; } = [];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiBeatmapset? Beatmapset { get; set; }
    
    [JsonConstructor]
    public ApiBeatmap() { }

    public ApiBeatmap(
        BeatmapDto beatmap,
        ApiBeatmapset? beatmapset = null
    ) : base(beatmap) {
        Failtimes = new Failtime
        {
            Fail = beatmap.Fails,
            Exit = beatmap.Exits
        };
        Owners =
        [
            new Owner { Id = beatmap.OwnerId, Username = beatmap.OwnerName },
            .. beatmap.Collaborators.Select(c => new Owner { Id = c.OwnerId, Username = c.OwnerName })
        ];
        
        Beatmapset = beatmapset;
    }

    public ApiBeatmap(
        Beatmap beatmap,
        ApiBeatmapset? beatmapset = null
    ) : base(beatmap) {
        Failtimes = new Failtime
        {
            Fail = beatmap.Fails,
            Exit = beatmap.Exits
        };
        Owners =
        [
            new Owner { Id = beatmap.OwnerId, Username = beatmap.OwnerName },
            .. beatmap.Collaborators.Select(c => new Owner { Id = c.Id, Username = c.Username })
        ];
        
        Beatmapset = beatmapset;
    }
}