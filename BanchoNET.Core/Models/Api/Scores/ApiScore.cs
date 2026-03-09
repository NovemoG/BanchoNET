using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Mods;

namespace BanchoNET.Core.Models.Api.Scores;

public class ApiScore
{
    public int ClassicTotalScore { get; set; }
    public bool Preserve { get; set; }
    public bool Processed { get; set; }
    public bool Ranked { get; set; }
    public MaxStatistics MaximumStatistics { get; set; }
    public LazerMod[] Mods { get; set; } = [];
    public Statistics Statistics { get; set; } = new();
    public int TotalScoreWithoutMods { get; set; }
    public int BeatmapId { get; set; }
    public long? BestId { get; set; }
    public long Id { get; set; }
    public required string Rank { get; set; }
    public string Type { get; set; }
    public int UserId { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public bool HasReplay { get; set; }
    public bool IsPerfectCombo { get; set; }
    public bool LegacyPerfect { get; set; }
    public long? LegacyScoreId { get; set; }
    public int? LegacyTotalScore { get; set; }
    public int MaxCombo { get; set; }
    public bool Passed { get; set; }
    public double? Pp { get; set; }
    public int RulesetId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int TotalScore { get; set; }
    public bool Replay { get; set; }
    public Attributes CurrentUserAttributes { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BasicApiPlayer? User { get; set; }
}