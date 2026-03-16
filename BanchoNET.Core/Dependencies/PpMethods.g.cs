#pragma warning disable CS8500
#pragma warning disable CS8981
using System;
using System.Runtime.InteropServices;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace Pp;

#nullable enable

public static partial class PpMethods
{
    const string __DllName = "libpp_cs";

    [DllImport(__DllName, EntryPoint = "compute_pp", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern double ComputePp(
        string path,
        byte mode,
        uint mods,
        uint combo,
        double acc,
        uint n300,
        uint n_geki,
        uint n100,
        uint n_katu,
        uint n50,
        uint n_misses,
        double clock_rate,
        bool is_da,
        float cs,
        float ar,
        float od,
        bool lazer
    );

    public static float ComputeScorePp(
        Beatmap beatmap,
        Score score
    ) {
        return (float)ComputePp(
            Storage.GetBeatmapPath(beatmap.Id),
            (byte)score.Mode.AsVanilla(),
            (uint)score.Mods,
            (uint)score.MaxCombo,
            score.Acc,
            (uint)score.Count300,
            (uint)score.Gekis,
            (uint)score.Count100,
            (uint)score.Katus,
            (uint)score.Count50,
            (uint)score.Misses,
            clock_rate: 1f,
            is_da: false,
            beatmap.Cs,
            beatmap.Ar,
            beatmap.Od,
            lazer: false
        );
    }
    
    public static float ComputeScorePp(
        int beatmapId,
        ApiScore score,
        float clockRate,
        bool lazer,
        bool isDa,
        float cs,
        float ar,
        float od
    ) {
        var stats = score.Statistics;
        
        return (float)ComputePp(
            Storage.GetBeatmapPath(beatmapId),
            (byte)score.RulesetId,
            (uint)score.LegacyMods,
            (uint)score.MaxCombo,
            score.Accuracy,
            (uint)(stats.Great ?? 0),
            (uint)(stats.LargeTickHit ?? 0),
            (uint)(stats.Ok ?? 0),
            (uint)(stats.SliderTailHit ?? 0),
            (uint)(stats.Meh ?? 0),
            (uint)(stats.Miss ?? 0),
            clock_rate: clockRate,
            is_da: isDa,
            cs,
            ar,
            od,
            lazer: lazer
        );
    }

    public static float ComputeNoMissesScorePp(
        Beatmap beatmap,
        Score score,
        int maxCombo
    ) {
        var acc = ScoreExtensions.CalculateAccuracy(
            score.Mode,
            score.Mods,
            score.Count300 + score.Misses,
            score.Count100,
            score.Count50,
            0,
            score.Gekis,
            score.Katus
        );
            
        return (float)ComputePp(
            Storage.GetBeatmapPath(beatmap.Id),
            (byte)score.Mode.AsVanilla(),
            (uint)score.Mods,
            (uint)maxCombo,
            acc,
            (uint)(score.Count300 + score.Misses),
            (uint)score.Gekis,
            (uint)score.Count100,
            (uint)score.Katus,
            (uint)score.Count50,
            (uint)0,
            clock_rate: 1f,
            is_da: false,
            beatmap.Cs,
            beatmap.Ar,
            beatmap.Od,
            lazer: false
        );
    }

    public static float ComputeNoMissesScorePp(
        Beatmap beatmap,
        ScoreDto score,
        int maxCombo
    ) {
        var acc = ScoreExtensions.CalculateAccuracy(
            (GameMode)score.Mode,
            (LegacyMods)score.Mods,
            score.Count300 + score.Misses,
            score.Count100,
            score.Count50,
            0,
            score.Gekis,
            score.Katus);
            
        return (float)ComputePp(
            Storage.GetBeatmapPath(beatmap.Id),
            score.Mode,
            (uint)score.Mods,
            (uint)maxCombo,
            acc,
            (uint)(score.Count300 + score.Misses),
            (uint)score.Gekis,
            (uint)score.Count100,
            (uint)score.Katus,
            (uint)score.Count50,
            (uint)0,
            clock_rate: 1f,
            is_da: false,
            beatmap.Cs,
            beatmap.Ar,
            beatmap.Od,
            lazer: false
        );
    }
}