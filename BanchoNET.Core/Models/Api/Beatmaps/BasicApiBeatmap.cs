using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Beatmaps;

public class BasicApiBeatmap
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
    public int? MaxCombo { get; set; }
    public bool IsScoreable { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public int ModeInt { get; set; }
    public long Passcount { get; set; }
    public long Playcount { get; set; }
    public int Ranked { get; set; }
    public string Url { get; set; }
    public string Checksum { get; set; }
    
    [JsonConstructor]
    public BasicApiBeatmap() { }

    public BasicApiBeatmap(
        BeatmapDto beatmap
    ) {
        BeatmapsetId = beatmap.SetId;
        DifficultyRating = beatmap.StarRating;
        Id = beatmap.Id;
        Mode = EnumExtensions.FromModeMap[beatmap.Mode];
        Status = beatmap.Status.ToApiBeatmapStatus();
        TotalLength = beatmap.TotalLength;
        UserId = beatmap.OwnerId;
        Version = beatmap.Version;
        Accuracy = beatmap.Od;
        Ar = beatmap.Ar;
        Bpm = beatmap.Bpm;
        Convert = false;
        CountCircles = beatmap.CirclesCount;
        CountSliders = beatmap.SlidersCount;
        CountSpinners = beatmap.SpinnersCount;
        Cs = beatmap.Cs;
        DeletedAt = null; //TODO
        Drain = beatmap.Hp;
        HitLength = beatmap.HitLength;
        MaxCombo = beatmap.MaxCombo;
        IsScoreable = beatmap.IsScoreable;
        LastUpdated = beatmap.LastUpdated;
        ModeInt = (int)beatmap.Mode;
        Passcount = beatmap.Passes;
        Playcount = beatmap.Plays;
        Ranked = (int)beatmap.Status;
        Url = $"https://osu.{AppSettings.Domain}/beatmaps/{beatmap.Id}";
        Checksum = beatmap.MD5;
    }

    public BasicApiBeatmap(
        Beatmap beatmap
    ) {
        BeatmapsetId = beatmap.BeatmapsetId;
        DifficultyRating = beatmap.StarRating;
        Id = beatmap.Id;
        Mode = EnumExtensions.FromModeMap[beatmap.Mode];
        Status = beatmap.Status.ToApiBeatmapStatus();
        TotalLength = beatmap.TotalLength;
        UserId = beatmap.OwnerId;
        Version = beatmap.Version;
        Accuracy = beatmap.Od;
        Ar = beatmap.Ar;
        Bpm = beatmap.Bpm;
        Convert = false;
        CountCircles = beatmap.CirclesCount;
        CountSliders = beatmap.SlidersCount;
        CountSpinners = beatmap.SpinnersCount;
        Cs = beatmap.Cs;
        DeletedAt = null; //TODO
        Drain = beatmap.Hp;
        HitLength = beatmap.HitLength;
        MaxCombo = beatmap.MaxCombo;
        IsScoreable = beatmap.IsScoreable;
        LastUpdated = beatmap.LastUpdated;
        ModeInt = (int)beatmap.Mode;
        Passcount = beatmap.Passes;
        Playcount = beatmap.Plays;
        Ranked = (int)beatmap.Status;
        Url = $"https://osu.{AppSettings.Domain}/beatmaps/{beatmap.Id}";
        Checksum = beatmap.Checksum;
    }
}