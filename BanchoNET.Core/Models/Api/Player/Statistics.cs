using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Dtos;

namespace BanchoNET.Core.Models.Api.Player;

public class Statistics
{
    public long Count100 { get; set; }
    public long Count300 { get; set; }
    public long Count50 { get; set; }
    public long CountMiss { get; set; }
    public Level Level { get; set; } = new();
    public int? GlobalRank { get; set; }
    public double? GlobalRankPercent { get; set; }
    public object? GlobalRankExp { get; set; } //TODO
    public float Pp { get; set; }
    public int PpExp { get; set; }
    public long RankedScore { get; set; }
    public double HitAccuracy { get; set; }
    public double Accuracy { get; set; }
    public int PlayCount { get; set; }
    public int PlayTime { get; set; }
    public long TotalScore { get; set; }
    public long TotalHits { get; set; }
    public int MaximumCombo { get; set; }
    public int ReplaysWatchedByOthers { get; set; }
    public bool IsRanked { get; set; }
    public GradeCounts GradeCounts { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RankChangeSince30Days { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BasicApiPlayer? User { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CountryRank { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Rank? Rank { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Variant[]? Variants { get; set; } = null; //TODO 4k/7k mania
    
    [JsonConstructor]
    public Statistics() { }

    public Statistics(
        PlayerRankingDto dto,
        int globalRank
    ) {
        var stats = dto.Stats;
        
        Count100 = stats.Total100s;
        Count300 = stats.Total300s;
        Count50 = stats.Total50s;
        GlobalRank = globalRank;
        Pp = stats.PP;
        RankedScore = stats.RankedScore;
        HitAccuracy = stats.Accuracy;
        Accuracy = stats.Accuracy / 100f;
        PlayCount = stats.PlayCount;
        PlayTime = stats.PlayTime;
        TotalScore = stats.TotalScore;
        TotalHits = (GameMode)stats.Mode switch
        {
            GameMode.VanillaStd => stats.TotalStdHits,
            GameMode.VanillaTaiko => stats.TotalTaikoHits,
            GameMode.VanillaCatch => stats.TotalCatchHits,
            GameMode.VanillaMania => stats.TotalManiaHits,
            _ => stats.TotalStdHits
        };
        MaximumCombo = stats.MaxCombo;
        ReplaysWatchedByOthers = stats.ReplayViews;
        IsRanked = stats.IsRanked;
        GradeCounts = new GradeCounts
        {
            Ss = stats.XCount,
            Ssh = stats.XHCount,
            S = stats.SCount,
            Sh = stats.SHCount,
            A = stats.ACount,
        };
        RankChangeSince30Days = dto.RankChangeSince30Days;
        User = new BasicApiPlayer(dto.Player);
    }
}