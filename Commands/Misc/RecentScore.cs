﻿using AkatsukiPp;
using BanchoNET.Attributes;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Objects.Scores;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("recent",
        Privileges.Unrestricted,
        "Displays your (or provided player's) most recent score in chat. Syntax: recent [<username>]",
        aliases: ["rs"])]
    private async Task<(bool, string)> RecentScore(params string[] args)
    {
        Player? player;
        Score? score;
        if (args.Length == 1)
        {
            player = await players.GetPlayerOrOffline(args[0]);

            if (player == null)
                return (true, "Player not found. Make sure you provided correct username.");

            if (player.IsBot)
                return (true, "Sadly, bots can't play the game \ud83d\ude2d");

            score = player.RecentScore ?? await scores.GetPlayerRecentScore(player.Id);
        }
        else
        {
            player = _playerCtx;
            score = _playerCtx.RecentScore ?? await scores.GetPlayerRecentScore(_playerCtx.Id);
        }
        
        if (score == null)
            return (true, "No recent scores.");

        player.RecentScore = score;
        
        var beatmap = await beatmaps.GetBeatmap(beatmapMD5: score.BeatmapMD5!);
        if (beatmap == null)
            return (true, "Beatmap not found.");
        
        var fcPP = 0.0f;
        if (score.Misses > 0 || beatmap.MaxCombo - score.MaxCombo > 15)
            if (await beatmapHandler.EnsureLocalBeatmapFile(beatmap.MapId, beatmap.MD5))
                fcPP = AkatsukiPpMethods.ComputeNoMissesScorePp(beatmap.MapId, score, beatmap.MaxCombo);
        
        var completionString = $"{score.Grade}";
        if (score.Status == (byte)SubmissionStatus.Failed)
        {
            completionString = beatmap.NotesCount > 0
                ? $"FAILED ({score.CalculateCompletion(beatmap):F2}%)"
                : "FAILED";
        }
        
        var notesHitString = $"{score.Count300}/{score.Count100}/{score.Count50}/{score.Misses}";
        var modsString = score.Mods.ToShortString();

        return (false,
            $"[{score.ModeToString()}] {player.Username}'s recent score on {beatmap.Embed()}{(modsString.Length > 0 ? $" +{modsString}" : "")}" +
            $"\n                {completionString} | {score.PP:F2}pp{(fcPP > 0 ? $" (if fc: {fcPP:F2}pp)" : "")} | " +
            $"{score.Acc:F2}% | {score.TotalScore.SplitNumber()} | x{score.MaxCombo}/{beatmap.MaxCombo} | {notesHitString}");
    }
}