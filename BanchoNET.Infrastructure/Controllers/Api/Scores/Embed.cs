using System.Net;
using System.Text;
using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Scores;

public partial class ScoresController
{
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    // Anonymous requests never reach RequireUser, but a request that does carry a client
    // credentials token still would, and this page is public either way.
    [AllowClientCredentials]
    public async Task<IActionResult> GetEmbed(
        long id
    ) {
        var score = await scores.GetScore(id);
        if (score == null) return NotFound();

        var player = await Players.GetPlayerOrOffline(score.PlayerId);
        if (player == null) return NotFound();
        
        var beatmap = await Beatmaps.GetBeatmap(score.MapId);
        if (beatmap == null) return NotFound();
        
        var title = WebUtility.HtmlEncode($"{player.Username}'s score on {beatmap.FullName()}");
        var description = WebUtility.HtmlEncode(
            $"{score.Grade} » {score.MaxCombo}/{beatmap.MaxCombo} » {score.Acc:0.00}% » {score.PP:0.##}pp");
        
        description += string.IsNullOrWhiteSpace(score.LazerMods)
            ? string.Empty
            : AppendModsString(score.LazerMods.ToMods());

        description += AppendStatistics(score.Statistics, score.LazerMods?.ToMods(), beatmap.HitLength, beatmap.Bpm, beatmap.Cs, beatmap.Ar, beatmap.Od, beatmap.Hp);
        
        var imageUrl = $"https://assets.ppy.sh/beatmaps/{beatmap.Set.Id}/covers/list.jpg";
        var canonicalUrl = $"https://osu.{AppSettings.Domain}/scores/{score.Id}";
        var targetUrl = beatmap.Url();
        
        var html = $"""
                     <!doctype html>
                     <html lang="en">
                     <head>
                       <meta charset="utf-8" />
                       <meta name="viewport" content="width=device-width, initial-scale=1.0" />

                       <title>{title}</title>
                       <meta name="description" content="{description}" />

                       <meta property="og:type" content="website" />
                       <meta property="og:title" content="{title}" />
                       <meta property="og:description" content="{description}" />
                       <meta property="og:url" content="{canonicalUrl}" />
                       <meta property="og:image" content="{imageUrl}" />
                       
                       <meta http-equiv="refresh" content="0;url={targetUrl}" />
                       <script>
                         window.location.replace({System.Text.Json.JsonSerializer.Serialize(targetUrl)});
                       </script>
                     </head>
                     <body>
                       <p>Redirecting…</p>
                     </body>
                     </html>
                     """;

        return Content(html, "text/html; charset=utf-8");
    }

    private static string AppendModsString(
        Mod[] mods
    ) {
        var sb = new StringBuilder(" » +");

        foreach (var mod in mods)
        {
            sb.Append(mod.Acronym);

            if (mod.Acronym is "DT" or "NC" && mod.Settings.TryGetValue("speed_change", out var rateChange))
                sb.Append($"({double.Parse((string)rateChange)}x)");
        }

        return sb.ToString();
    }

    private static string AppendStatistics(
        Dictionary<HitResult, int> stats,
        Mod[]? mods,
        int hitLength,
        float bpm,
        float cs,
        float ar,
        float od,
        float hp
    ) {
        var clockRate = 1f;
        var hasDt = false;
        var hasHr = false;
        var da = mods?.FirstOrDefault(m => m.Acronym == "DA");

        if (mods != null)
        {
            var dt = mods.FirstOrDefault(m => m.Acronym is "DT" or "NC");
            var ht = mods.FirstOrDefault(m => m.Acronym is "HT" or "DC");
            if (dt != null)
            {
                clockRate = dt.Settings.TryGetValue("speed_change", out var speed)
                    ? float.Parse((string)speed)
                    : 1.5f;

                hasDt = true;
            }
            else if (ht != null)
            {
                clockRate = ht.Settings.TryGetValue("speed_change", out var speed)
                    ? float.Parse((string)speed)
                    : 0.75f;

                hasDt = true;
            }

            hasHr = mods.Any(m => m.Acronym == "HR");
        }
        
        if (da is { Settings.Count: > 0 })
        {
            cs = da.Settings.TryGetValue("circle_size", out var csValue) ? float.Parse((string)csValue) : cs;
            ar = da.Settings.TryGetValue("approach_rate", out var arValue) ? float.Parse((string)arValue) : ar;
            od = da.Settings.TryGetValue("overall_difficulty", out var odValue) ? float.Parse((string)odValue) : od;
            hp = da.Settings.TryGetValue("drain_rate", out var hpValue) ? float.Parse((string)hpValue) : hp;
        }
        else if (hasHr)
        {
            cs = MathF.Min(cs * 1.3f, 10f);
            ar = MathF.Min(ar * 1.4f, 10f);
            od = MathF.Min(od * 1.4f, 10f);
            hp = MathF.Min(hp * 1.4f, 10f);
        }
        
        if (hasDt)
        {
            ar = ApplyClockRateToAr(ar, clockRate);
            od = ApplyClockRateToOd(od, clockRate);
            hitLength = (int)MathF.Round(hitLength / clockRate);
            bpm *= clockRate;
        }
        
        var sb = new StringBuilder($" » BPM: {bpm}\n{hitLength / 60}:{hitLength % 60:D2} » ");

        sb.Append(stats.TryGetValue(HitResult.Great, out var count300) ? $"{count300} / " : "0 / ");
        sb.Append(stats.TryGetValue(HitResult.Ok, out var count100) ? $"{count100} / " : "0 / ");
        sb.Append(stats.TryGetValue(HitResult.Meh, out var count50) ? $"{count50} / " : "0 / ");
        sb.Append(stats.TryGetValue(HitResult.Miss, out var countMiss) ? $"{countMiss}x" : "0x");
        
        sb.Append($" » CS: {cs:0.##} AR: {ar:0.##} OD: {od:0.##} HP: {hp:0.##}");

        return sb.ToString();
    }

    private static float ApplyClockRateToAr(
        float ar,
        float clockRate
    ) {
        var preempt = ar < 5f
            ? 1800f - 120f * ar
            : 1200f - 150f * (ar - 5f);
        
        preempt /= clockRate;
        
        return preempt > 1200f
            ? (1800f - preempt) / 120f
            : 5f + (1200f - preempt) / 150f;
    }

    private static float ApplyClockRateToOd(
        float od,
        float clockRate
    ) {
        var greatWindow = 80f - 6f * od;
        greatWindow /= clockRate;

        return (80f - greatWindow) / 6f;
    }
}