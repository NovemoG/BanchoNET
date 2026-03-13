using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class ApiBeatmap
{
    public int BeatmapsetId { get; set; }
    public double DifficultyRating { get; set; }
    public int Id { get; set; }
    public string Mode { get; set; }
    public string Status { get; set; }
    public int TotalLength { get; set; }
    public int UserId { get; set; }
    public string Version { get; set; }
    public float Accuracy { get; set; }
    public float Ar { get; set; }
    public double Bpm { get; set; }
    public bool Convert { get; set; }
    public int CountCircles { get; set; }
    public int CountSliders { get; set; }
    public int CountSpinners { get; set; }
    public float Cs { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public float Drain { get; set; }
    public int HitLength { get; set; }
    public bool IsScoreable { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public int ModeInt { get; set; }
    public long Passcount { get; set; }
    public long Playcount { get; set; }
    public int Ranked { get; set; }
    public string Url { get; set; }
    public string Checksum { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiBeatmapset? Beatmapset { get; set; }
    public int CurrentUserPlaycount { get; set; }
    public string[] CurrentUserTagIds { get; set; } = [];
    public Failtime Failtimes { get; set; }
    public int MaxCombo { get; set; }
    public List<Owner>? Owners { get; set; }
    public List<MapTag> TopTagIds { get; set; } = [];
    
    [JsonConstructor]
    public ApiBeatmap() { }

    public ApiBeatmap(
        BeatmapDto mapDto,
        ApiBeatmapset? beatmapset = null
    ) {
        BeatmapsetId = mapDto.SetId;
        DifficultyRating = mapDto.StarRating;
        Id = mapDto.MapId;
        Mode = EnumExtensions.FromModeMap[(GameMode)mapDto.Mode];
        Status = ((BeatmapStatus)mapDto.Status).ToApiBeatmapStatus();
        TotalLength = mapDto.TotalLength;
        UserId = mapDto.CreatorId;
        Version = mapDto.Name;
        Accuracy = mapDto.Od;
        Ar = mapDto.Ar;
        Bpm = mapDto.Bpm;
        Convert = false; //TODO
        CountCircles = mapDto.CirclesCount;
        CountSliders = mapDto.SlidersCount;
        CountSpinners = mapDto.SpinnersCount;
        Cs = mapDto.Cs;
        DeletedAt = null; //TODO
        Drain = mapDto.Hp;
        HitLength = mapDto.HitLength;
        IsScoreable = true; //TODO
        LastUpdated = mapDto.LastUpdate;
        ModeInt = mapDto.Mode;
        Passcount = mapDto.Passes;
        Playcount = mapDto.Plays;
        Ranked = mapDto.Status == (sbyte)BeatmapStatus.Ranked ? 1 : 0;
        Url = $"https://osu.ppy.sh/beatmaps/{mapDto.MapId}";
        Checksum = mapDto.MD5;
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
    ) {
        BeatmapsetId = beatmap.SetId;
        DifficultyRating = beatmap.StarRating;
        Id = beatmap.Id;
        Mode = EnumExtensions.FromModeMap[beatmap.Mode];
        Status = beatmap.Status.ToApiBeatmapStatus();
        TotalLength = beatmap.TotalLength;
        UserId = beatmap.CreatorId;
        Version = beatmap.Name;
        Accuracy = beatmap.Od;
        Ar = beatmap.Ar;
        Bpm = beatmap.Bpm;
        Convert = false; //TODO
        CountCircles = beatmap.CirclesCount;
        CountSliders = beatmap.SlidersCount;
        CountSpinners = beatmap.SpinnersCount;
        Cs = beatmap.Cs;
        DeletedAt = null; //TODO
        Drain = beatmap.Hp;
        HitLength = beatmap.HitLength;
        IsScoreable = true; //TODO
        LastUpdated = beatmap.LastUpdate;
        ModeInt = (int)beatmap.Mode;
        Passcount = beatmap.Passes;
        Playcount = beatmap.Plays;
        Ranked = beatmap.Status == BeatmapStatus.Ranked ? 1 : 0;
        Url = $"https://osu.ppy.sh/beatmaps/{beatmap.Id}";
        Checksum = beatmap.MD5;
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