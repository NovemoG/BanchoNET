using System.Text.Json.Serialization;

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
    public int? CountryRank { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Rank? Rank { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Variant[]? Variants { get; set; } = null; //TODO 4k/7k mania
}