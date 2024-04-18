﻿using AkatsukiPp;
using BanchoNET.Models.Dtos;
using BanchoNET.Objects;
using BanchoNET.Objects.Scores;

namespace BanchoNET.Utils;

public static class ScoreExtensions
{
    public static string ComputeOnlineChecksum(this Score score,
        string osuVersion,
        string clientHash,
        string storyboardChecksum)
    {
        if (score.Player == null || score.Beatmap == null) return "";

        return string.Format(
            "chickenmcnuggets{0}o15{1}{2}smustard{3}{4}uu{5}{6}{7}{8}{9}{10}{11}Q{12}{13}{15}{14:yyMMddHHmmss}{16}{17}",
            score.Count300 + score.Count100,
            score.Count50,
            score.Gekis,
            score.Katus,
            score.Misses,
            score.Beatmap.MD5,
            score.MaxCombo,
            score.Perfect,
            score.Player.Username,
            score.TotalScore,
            score.Grade.ToString(),
            (int)score.Mods,
            score.Passed,
            (int)score.Mode.AsVanilla(),
            score.ClientTime,
            osuVersion,
            clientHash,
            storyboardChecksum).CreateMD5();
    }

    public static void CalculateAccuracy(this Score score)
    {
        score.Acc = CalculateAccuracy(
            score.Mode,
            score.Mods,
            score.Count300,
            score.Count100,
            score.Count50,
            score.Misses,
            score.Gekis,
            score.Katus);
    }

    public static float CalculateAccuracy(
        GameMode mode,
        Mods mods,
        int count300,
        int count100,
        int count50,
        int misses,
        int gekis,
        int katus)
    {
        int objects;
        float acc;

        switch (mode.AsVanilla())
        {
            case GameMode.VanillaStd:
                objects = count300 + count100 + count50 + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count300 * 300f + count100 * 100f + count50 * 50f) / (objects * 300f);
                break;
            case GameMode.VanillaTaiko:
                objects = count300 + count100 + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count100 * 0.5f + count300) / objects;
                break;
            case GameMode.VanillaCatch:
                objects = count300 + count100 + count50 + katus + misses;

                if (objects == 0) return 0f;

                acc = 100f * (count300 + count100 + count50) / objects;
                break;
            case GameMode.VanillaMania:
                objects = count300 + count100 + count50 + gekis + katus + misses;

                if (objects == 0) return 0f;

                if ((mods & Mods.ScoreV2) == Mods.ScoreV2)
                {
                    return 100f *
                           (gekis * 305f + count300 * 300f + katus * 200f + count100 * 100f + count50 * 50f) /
                           (objects * 305f);
                }

                acc = 100f *
                      ((count300 + gekis) * 300f + katus * 200f + count100 * 100f + count50 * 50f) /
                      (objects * 300f);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), $"Invalid mode {mode}");
        }

        return acc;
    }

    public static void CalculatePerformance(this Score score, int beatmapId)
    {
        if (score.Mods.HasMod(Mods.NightCore))
            score.Mods |= Mods.DoubleTime;

        var pp = AkatsukiPpMethods.ComputeScorePp(Storage.GetBeatmapPath(beatmapId), score);

        score.PP = double.IsInfinity(pp) || double.IsNaN(pp) ? 0.0f : MathF.Round(pp, 5);

        Console.WriteLine($"[Score Extensions] Submitted score pp: {score.PP}");
    }
    
    public static void ComputeSubmissionStatus(this Score score, Score? prevBest, Score? prevBestWithMods)
    {
        // TODO: Probably change it somehow
        
        if (prevBest == null)
        {
            score.Status = SubmissionStatus.Best;
            return;
        }

        if (prevBestWithMods == null || prevBest.Id == prevBestWithMods.Id)
        {
            var equalMods = score.Mods == prevBest.Mods;
            score.PreviousBest = prevBest;
            score.Status = equalMods
                ? SubmissionStatus.Submitted
                : SubmissionStatus.BestWithMods;

            if (AppSettings.SubmitByPP)
            {
                if (!(score.PP > prevBest.PP)) return;
        
                score.Status = SubmissionStatus.Best;
                prevBest.Status = equalMods
                    ? SubmissionStatus.Submitted
                    : SubmissionStatus.BestWithMods;
            }
            else
            {
                if (score.TotalScore < prevBest.TotalScore) return;
        
                score.Status = SubmissionStatus.Best;
                prevBest.Status = equalMods
                    ? SubmissionStatus.Submitted
                    : SubmissionStatus.BestWithMods;
            }
        }
        else
        {            
            var equalMods = score.Mods == prevBest.Mods;
        
            score.PreviousBest = prevBest;
            score.Status = equalMods
                ? SubmissionStatus.Submitted
                : SubmissionStatus.BestWithMods;

            if (AppSettings.SubmitByPP)
            {
                if (!(score.PP > prevBest.PP))
                {
                    if (score.PP > prevBestWithMods.PP)
                        prevBestWithMods.Status = SubmissionStatus.Submitted;
                    
                    return;
                }
        
                score.Status = SubmissionStatus.Best;
                prevBest.Status = equalMods
                    ? SubmissionStatus.Submitted
                    : SubmissionStatus.BestWithMods;
                
                if (score.Mods == prevBestWithMods.Mods && score.PP > prevBestWithMods.PP)
                    prevBestWithMods.Status = SubmissionStatus.Submitted;
            }
            else
            {
                if (score.TotalScore < prevBest.TotalScore)
                {
                    if (score.TotalScore > prevBestWithMods.TotalScore)
                        prevBestWithMods.Status = SubmissionStatus.Submitted;
                    
                    return;
                }
        
                score.Status = SubmissionStatus.Best;
                prevBest.Status = equalMods
                    ? SubmissionStatus.Submitted
                    : SubmissionStatus.BestWithMods;

                if (score.Mods == prevBestWithMods.Mods && score.TotalScore > prevBestWithMods.TotalScore)
                    prevBestWithMods.Status = SubmissionStatus.Submitted;
            }
        }
    }
}