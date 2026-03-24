using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ApiBeatmap : BasicApiBeatmap
{
    public int CurrentUserPlaycount { get; set; }
    public string[] CurrentUserTagIds { get; set; } = [];
    public Failtime Failtimes { get; set; }
    public int MaxCombo { get; set; }
    public List<Owner>? Owners { get; set; }
    public List<MapTag> TopTagIds { get; set; } = [];
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiBeatmapset? Beatmapset { get; set; }
    
    [JsonConstructor]
    public ApiBeatmap() { }

    public ApiBeatmap(
        BeatmapDto mapDto,
        ApiBeatmapset? beatmapset = null
    ) : base(mapDto) {
        CurrentUserPlaycount = 0; //TODO fetch
        CurrentUserTagIds = []; //TODO
        Failtimes = new Failtime
        {
            Fail = [], //TODO
            Exit = [] //TODO
        };
        MaxCombo = mapDto.MaxCombo;
        Owners = []; //TODO
        TopTagIds = []; //TODO

        Beatmapset = beatmapset;
    }

    public ApiBeatmap(
        Beatmap beatmap,
        ApiBeatmapset? beatmapset = null
    ) : base(beatmap) {
        CurrentUserPlaycount = 0; //TODO fetch
        CurrentUserTagIds = []; //TODO
        Failtimes = new Failtime
        {
            Fail = [], //TODO
            Exit = [] //TODO
        };
        MaxCombo = beatmap.MaxCombo;
        Owners = []; //TODO
        TopTagIds = []; //TODO
        
        Beatmapset = beatmapset;
    }
}