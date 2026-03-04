namespace BanchoNET.Core.Models.Api.Player;

public class Statistics
{
    public int Count100 { get; set; }
    public int Count300 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }
    public Level Level { get; set; } = new();
    public int GlobalRank { get; set; }
    public double GlobalRankPercent { get; set; }
    public object? GlobalRankExp { get; set; } //TODO
    public double Pp { get; set; }
    public int PpExp { get; set; }
    public long RankedScore { get; set; }
    public double HitAccuracy { get; set; }
    public double Accuracy { get; set; }
    public int PlayCount { get; set; }
    public int PlayTime { get; set; }
    public long TotalScore { get; set; }
    public int TotalHits { get; set; }
    public int MaximumCombo { get; set; }
    public int ReplaysWatchedByOthers { get; set; }
    public bool IsRanked { get; set; }
    public GradeCounts GradeCounts { get; set; } = new();
    public int CountryRank { get; set; }
    public Rank Rank { get; set; } = new();
}