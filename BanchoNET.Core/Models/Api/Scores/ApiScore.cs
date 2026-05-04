using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Scores;

public class ApiScore
{
    public int ClassicTotalScore { get; set; }
    public bool Preserve { get; set; }
    public bool Processed { get; set; }
    public bool Ranked { get; set; }
    public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new();
    public Dictionary<HitResult, int> Statistics { get; set; } = new();
    public Mod[] Mods { get; set; } = [];
    public int TotalScoreWithoutMods { get; set; }
    public int BeatmapId { get; set; }
    public long? BestId { get; set; }
    public long Id { get; set; }
    public string Rank { get; set; } = null!;
    [JsonIgnore] public Grade Grade { get; set; }
    public string Type { get; set; } = "solo_score"; //TODO
    public int UserId { get; set; }
    public double Accuracy { get; set; }
    public int BuildId { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public bool HasReplay { get; set; }
    public bool IsPerfectCombo { get; set; }
    public bool LegacyPerfect { get; set; }
    public long? LegacyScoreId { get; set; }
    public int? LegacyTotalScore { get; set; }
    public int MaxCombo { get; set; }
    public bool Passed { get; set; }
    public double Pp { get; set; }
    public int RulesetId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public int TotalScore { get; set; }
    public bool Replay { get; set; }
    public Attributes CurrentUserAttributes { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BasicApiPlayer? User { get; set; }
    
    [JsonIgnore] public LegacyMods LegacyMods { get; set; }
    [JsonIgnore] public string ModKeys { get; set; } = string.Empty;
    [JsonIgnore] public int Combo { get; set; }
    [JsonIgnore] public int LeaderboardPosition { get; set; }
    [JsonIgnore] public SubmissionStatus Status { get; set; }
    [JsonIgnore] public int TimeElapsed { get; set; }
    [JsonIgnore] public double ClockRate { get; set; }
    [JsonIgnore] public ApiScore? PreviousBest { get; set; }
    [JsonIgnore] public int[] Pauses { get; init; } = [];
    
    [JsonConstructor]
    public ApiScore() { }

    private ApiScore(
        ScoreDto score,
        PlayerDto player
    ) {
        //TODO (for players that have Classic score enabled)
        ClassicTotalScore = AppSettings.SortLeaderboardByPP ? (int)MathF.Round(score.PP) : score.LegacyTotalScore;
        Preserve = score.Preserve;
        Processed = score.Processed;
        Ranked = score.Ranked;
        LegacyMods = score.Mods;
        Mods = score.LazerMods?.ToMods() ?? LegacyMods.ToLazerMods();
        ModKeys = score.ModKeys ?? "";
        Statistics = score.Statistics;
        TotalScoreWithoutMods = score.TotalScoreWithoutMods;
        Id = score.Id;
        Grade = score.Grade;
        Rank = Grade.ToString();
        UserId = score.PlayerId;
        Accuracy = score.Acc / 100f;
        EndedAt = score.PlayTime;
        HasReplay = score.HasReplay;
        IsPerfectCombo = score.IsPerfectCombo;
        LegacyPerfect = score.IsPerfectCombo;
        //TODO LegacyScoreId
        //TODO LegacyTotalScore
        MaxCombo = score.MaxCombo;
        Passed = score.Passed;
        Pp = score.PP;
        RulesetId = (int)score.Mode;
        StartedAt = score.StartTime;
        TotalScore = AppSettings.SortLeaderboardByPP ? (int)MathF.Round(score.PP) : score.LegacyTotalScore;
        Replay = HasReplay;
        //TODO CurrentUserAttributes pin
        Status = score.Status;
        
        User = new BasicApiPlayer(player);
    }

    public ApiScore(
        ScoreDto score,
        PlayerDto player,
        Beatmap beatmap
    ) : this(score, player) {
        MaximumStatistics = beatmap.MaxStatistics;
        BeatmapId = beatmap.Id;
    }

    public ApiScore(
        ScoreDto score,
        PlayerDto player,
        BeatmapDto beatmap
    ) : this(score, player) {
        MaximumStatistics = beatmap.MaximumStatistics;
        BeatmapId = beatmap.Id;
    }
}