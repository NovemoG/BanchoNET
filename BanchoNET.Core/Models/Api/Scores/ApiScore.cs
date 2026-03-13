using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Scores;

namespace BanchoNET.Core.Models.Api.Scores;

public class ApiScore
{
    public int ClassicTotalScore { get; set; }
    public bool Preserve { get; set; }
    public bool Processed { get; set; }
    public bool Ranked { get; set; }
    public MaxStatistics MaximumStatistics { get; set; } = new();
    public ApiMod[] Mods { get; set; } = [];
    public Statistics Statistics { get; set; } = new();
    public int TotalScoreWithoutMods { get; set; }
    public int BeatmapId { get; set; }
    public long? BestId { get; set; }
    public long Id { get; set; }
    public string Rank { get; set; } = null!;
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
    public double? Pp { get; set; }
    public int RulesetId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int TotalScore { get; set; }
    public bool Replay { get; set; }
    public Attributes CurrentUserAttributes { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BasicApiPlayer? User { get; set; }
    
    [JsonConstructor]
    public ApiScore() { }

    public ApiScore(
        Score score,
        Players.Player player,
        Beatmap beatmap
    ) {
        ClassicTotalScore = score.TotalScore; //TODO
        Preserve = score.Preserve;
        Processed = score.Processed;
        Ranked = score.Ranked;
        MaximumStatistics = beatmap.MaxStatistics;
        //TODO mods
        Statistics = new Statistics
        {
            Ok = score.Count100,
            Meh = score.Count50,
            Miss = score.Misses,
            Great = score.Count300,
            IgnoreHit = score.IgnoreHit,
            IgnoreMiss = score.IgnoreMiss,
            LargeTickHit = score.Gekis,
            SliderTailHit = score.Katus
        };
        //TODO TotalScoreWithoutMods
        BeatmapId = beatmap.Id;
        Id = score.Id;
        Rank = score.Grade.ToString();
        UserId = score.PlayerId;
        Accuracy = score.Acc / 100f;
        EndedAt = score.ClientTime;
        HasReplay = score.HasReplay;
        IsPerfectCombo = score.Perfect;
        LegacyPerfect = score.Perfect;
        //TODO LegacyScoreId
        //TODO LegacyTotalScore
        MaxCombo = score.MaxCombo;
        Passed = score.Passed;
        Pp = score.PP;
        RulesetId = (int)score.Mode;
        StartedAt = score.StartTime;
        TotalScore = score.TotalScore;
        Replay = HasReplay;
        //TODO CurrentUserAttributes
        
        User = new BasicApiPlayer(player);
    }

    public ApiScore(
        ScoreDto scoreDto,
        PlayerDto player,
        Beatmap beatmap
    ) {
        ClassicTotalScore = scoreDto.TotalScore; //TODO
        Preserve = scoreDto.Preserve;
        Processed = scoreDto.Processed;
        Ranked = scoreDto.Ranked;
        MaximumStatistics = beatmap.MaxStatistics;
        //TODO mods
        Statistics = new Statistics
        {
            Ok = scoreDto.Count100,
            Meh = scoreDto.Count50,
            Miss = scoreDto.Misses,
            Great = scoreDto.Count300,
            IgnoreHit = scoreDto.IgnoreHit,
            IgnoreMiss = scoreDto.IgnoreMiss,
            LargeTickHit = scoreDto.Gekis,
            SliderTailHit = scoreDto.Katus
        };
        //TODO TotalScoreWithoutMods
        BeatmapId = beatmap.Id;
        Id = scoreDto.Id;
        Rank = scoreDto.Grade.ToString();
        UserId = scoreDto.PlayerId;
        Accuracy = scoreDto.Acc / 100f;
        EndedAt = scoreDto.PlayTime;
        HasReplay = scoreDto.HasReplay;
        IsPerfectCombo = scoreDto.IsPerfectCombo;
        LegacyPerfect = scoreDto.IsPerfectCombo;
        //TODO LegacyScoreId
        //TODO LegacyTotalScore
        MaxCombo = scoreDto.MaxCombo;
        Passed = scoreDto.Passed;
        Pp = scoreDto.PP;
        RulesetId = scoreDto.Mode;
        StartedAt = scoreDto.StartTime;
        TotalScore = scoreDto.TotalScore;
        Replay = HasReplay;
        //TODO CurrentUserAttributes
        
        User = new BasicApiPlayer(player);
    }
}