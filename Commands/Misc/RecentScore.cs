using AkatsukiPp;
using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;
using BanchoNET.Objects.Scores;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("recent",
        Privileges.Unrestricted,
        "Displays your most recent score in chat.",
        aliases: ["r"])]
    private async Task<(bool, string)> RecentScore(params string[] args)
    {
        var score = _playerCtx.RecentScore ?? await scores.GetPlayerRecentScore(_playerCtx.Id);
        if (score == null)
            return (true, "No recent scores.");

        _playerCtx.RecentScore = score;
        
        var beatmap = await beatmaps.GetBeatmap(beatmapMD5: score.BeatmapMD5!);
        if (beatmap == null)
            return (true, "Beatmap not found.");
        
        var fcPP = 0.0f;
        if (await beatmapHandler.EnsureLocalBeatmapFile(beatmap.MapId, beatmap.MD5))
            fcPP = AkatsukiPpMethods.ComputeNoMissesScorePp(beatmap.MapId, score, beatmap.MaxCombo);

        var completionString = "";
        if (score.Status == (byte)SubmissionStatus.Failed)
        {
            completionString = beatmap.NotesCount > 0
                ? $"FAILED ({score.CalculateCompletion(beatmap):F2}%)"
                : "FAILED";
        }

        var modsString = score.Mods.ToShortString();

        return (false,
            $"{_playerCtx.Username}'s recent score on {beatmap.Embed()}{(modsString.Length > 0 ? $" +{modsString}" : "")}" +
            $"\n[{score.ModeToString()}] | x{score.MaxCombo}/{beatmap.MaxCombo} | {score.PP:F2}pp " +
            $"{(fcPP > 0 ? $"(if fc: {fcPP:F2}pp) " : "")}| {completionString}");
    }
}