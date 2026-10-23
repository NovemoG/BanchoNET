using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Utils.Scores;

public static class ScoreSubmissionMapping
{
    public static ScoreSubmission FromLegacy(
        Score score,
        Beatmap beatmap
    ) {
        score.CalculateAccuracy();
        
        if (score.Mods.HasMod(LegacyMods.NightCore))
            score.Mods |= LegacyMods.DoubleTime;
        
        var rate = score.Mods.ToLazerMods().GetClockRate();
        
        return new ScoreSubmission(new ScoreDto
        {
            BeatmapMD5 = beatmap.Checksum,
            MapId = beatmap.Id,
            PlayerId = score.PlayerId,
            LegacyTotalScore = score.TotalScore,
            MaxCombo = score.MaxCombo,
            Acc = score.Acc,
            Mods = score.Mods,
            ModKeys = score.Mods.ToKeysString(),
            LazerMods = null,
            Statistics = score.GetStatistics(),
            Grade = score.Grade,
            Status = score.Passed ? SubmissionStatus.Submitted : SubmissionStatus.Failed,
            Mode = score.Mode,
            PlayTime = score.ClientTime,
            TimeElapsed = score.TimeElapsed,
            ClientFlags = (int)score.ClientFlags,
            LegacyPerfect = score.Perfect,
            IsPerfectCombo = score.Perfect,
            OnlineChecksum = score.ClientChecksum
        }, ScoreProcessing.LegacyPlayTimeSeconds(score.TimeElapsed, rate));
    }

    public static ScoreSubmission FromLazer(
        ApiScore score,
        Beatmap beatmap
    ) {
        return new ScoreSubmission(new ScoreDto
        {
            Id = score.Id,
            BeatmapMD5 = beatmap.Checksum,
            MapId = beatmap.Id,
            PlayerId = score.UserId,
            TotalScore = score.TotalScore,
            TotalScoreWithoutMods = score.TotalScoreWithoutMods,
            MaxCombo = score.MaxCombo,
            Acc = (float)score.Accuracy,
            Mods = score.Mods.ToLegacyMods(),
            ModKeys = score.Mods.ToKeysString(),
            LazerMods = score.ModsToString(),
            Statistics = score.Statistics,
            Grade = score.Grade,
            Status = score.Passed ? SubmissionStatus.Submitted : SubmissionStatus.Failed,
            Mode = (GameMode)score.RulesetId,
            StartTime = score.StartedAt,
            PlayTime = score.EndedAt,
            LegacyPerfect = score.LegacyPerfect,
            IsPerfectCombo = score.IsPerfectCombo
        }, null, score.MaximumStatistics);
    }

    public static void Apply(
        ScoreDto stored,
        Score score
    ) {
        score.Id = stored.Id;
        score.PP = stored.PP;
        score.Status = stored.Status;
        score.TotalScoreStatus = stored.TotalScoreStatus;
        score.Preserve = stored.Preserve;
        score.Processed = stored.Processed;
        score.Ranked = stored.Ranked;
        score.HasReplay = stored.HasReplay;
        score.StandardisedTotalScore = stored.TotalScore;
        score.TotalScoreWithoutMods = stored.TotalScoreWithoutMods;
        score.TotalScore = stored.LegacyTotalScore;
        score.Acc = stored.Acc;
        score.MaxCombo = stored.MaxCombo;
        score.Count300 = stored.GetCount300();
        score.Count100 = stored.GetCount100();
        score.Count50 = stored.GetCount50();
        score.Gekis = stored.GetCountGeki();
        score.Katus = stored.GetCountKatu();
        score.Misses = stored.GetCountMiss();
        score.Mods = stored.Mods;
        score.ModKeys = stored.ModKeys;
        score.Grade = stored.Grade;
        score.Passed = stored.Passed;
    }

    public static void Apply(
        ScoreDto stored,
        ApiScore score
    ) {
        score.Id = stored.Id;
        score.Pp = stored.PP;
        score.Status = stored.Status;
        score.TotalScoreStatus = stored.TotalScoreStatus;
        score.Preserve = stored.Preserve;
        score.Processed = stored.Processed;
        score.Ranked = stored.Ranked;
        score.HasReplay = stored.HasReplay;
        score.TotalScore = stored.TotalScore;
        score.TotalScoreWithoutMods = stored.TotalScoreWithoutMods;
        score.EndedAt = stored.PlayTime;
        score.StartedAt = stored.StartTime;
        score.Accuracy = stored.Acc / 100d;
        score.Statistics = stored.Statistics;
        score.Mods = stored.LazerMods?.ToMods() ?? stored.Mods.ToLazerMods();
        score.ModKeys = stored.ModKeys;
        score.LegacyMods = stored.Mods;
        score.MaxCombo = stored.MaxCombo;
        score.RulesetId = (int)stored.Mode;
        score.BeatmapId = stored.MapId;
        score.UserId = stored.PlayerId;
        score.Grade = stored.Grade;
        score.Rank = stored.Grade.ToString();
        score.Passed = stored.Passed;
        score.IsPerfectCombo = stored.IsPerfectCombo;
        score.LegacyPerfect = stored.LegacyPerfect;
    }
}