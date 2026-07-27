#if !OFFICIAL_PP
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
        nint clock_rate,
        float cs,
        float ar,
        float od,
        bool lazer
    );
    
    [LibraryImport(__DllName, EntryPoint = "rosu_difficulty_graph_calculate_from_path", StringMarshalling = StringMarshalling.Utf8)]
    private static partial RosuDifficultyGraphNativeResult CalculateFromPathNative(
        string pathUtf8,
        uint mods
    );

    [LibraryImport(__DllName, EntryPoint = "rosu_difficulty_graph_string_free")]
    private static partial void FreeStringNative(
        IntPtr value
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
            nint.Zero,
            beatmap.Cs,
            beatmap.Ar,
            beatmap.Od,
            lazer: false
        );
    }
    
    public static unsafe float ComputeScorePp(
        int beatmapId,
        ApiScore score,
        double clockRate,
        bool isLazer,
        float cs,
        float ar,
        float od
    ) {
        return (float)ComputePp(
            Storage.GetBeatmapPath(beatmapId),
            (byte)score.RulesetId,
            (uint)score.LegacyMods,
            (uint)score.MaxCombo,
            score.Accuracy,
            (uint)score.GetCount300(),
            (uint)score.GetCountGeki(),
            (uint)score.GetCount100(),
            (uint)score.GetCountKatu(),
            (uint)score.GetCount50(),
            (uint)score.GetCountMiss(),
            (nint)(&clockRate),
            cs,
            ar,
            od,
            isLazer
        );
    }
    
    public static unsafe float ComputeScorePp(
        int beatmapId,
        ScoreDto score,
        double clockRate,
        bool isLazer,
        float cs,
        float ar,
        float od
    ) {
        return (float)ComputePp(
            Storage.GetBeatmapPath(beatmapId),
            (byte)score.Mode,
            (uint)score.Mods,
            (uint)score.MaxCombo,
            score.Acc,
            (uint)score.GetCount300(),
            (uint)score.GetCountGeki(),
            (uint)score.GetCount100(),
            (uint)score.GetCountKatu(),
            (uint)score.GetCount50(),
            (uint)score.GetCountMiss(),
            (nint)(&clockRate),
            cs,
            ar,
            od,
            isLazer
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
            nint.Zero,
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
            score.GetCount300() + score.GetCountMiss(),
            score.GetCount100(),
            score.GetCount50(),
            0,
            score.GetCountGeki(),
            score.GetCountKatu()
        );
            
        return (float)ComputePp(
            Storage.GetBeatmapPath(beatmap.Id),
            (byte)score.Mode,
            (uint)score.Mods,
            (uint)maxCombo,
            acc,
            (uint)(score.GetCount300() + score.GetCountMiss()),
            (uint)score.GetCountGeki(),
            (uint)score.GetCount100(),
            (uint)score.GetCountKatu(),
            (uint)score.GetCount50(),
            (uint)0,
            nint.Zero,
            beatmap.Cs,
            beatmap.Ar,
            beatmap.Od,
            lazer: false
        );
    }
    
    public static string CalculateGraphJson(
        int beatmapId,
        uint mods = 0
    ) {
        var result = CalculateFromPathNative(Storage.GetBeatmapPath(beatmapId), mods);

        try
        {
            var status = (RosuDifficultyGraphStatus)result.StatusCode;

            if (status == RosuDifficultyGraphStatus.Success)
                return Marshal.PtrToStringUTF8(result.Json) ?? string.Empty;

            var message = Marshal.PtrToStringUTF8(result.Error) ?? "Unknown native error";
            throw new RosuDifficultyGraphException(status, message);
        }
        finally
        {
            if (result.Json != IntPtr.Zero)
                FreeStringNative(result.Json);

            if (result.Error != IntPtr.Zero)
                FreeStringNative(result.Error);
        }
    }
    
    public enum RosuDifficultyGraphStatus
    {
        Success = 0,
        NullPointer = 1,
        InvalidUtf8 = 2,
        IoError = 3,
        DecodeError = 4,
        UnsupportedMode = 5,
        SerializeError = 6,
        Panic = 255,
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct RosuDifficultyGraphNativeResult
    {
        public readonly int StatusCode;
        public readonly IntPtr Json;
        public readonly IntPtr Error;
    }
    
    public sealed class RosuDifficultyGraphException : Exception
    {
        public RosuDifficultyGraphStatus Status { get; }

        public RosuDifficultyGraphException(RosuDifficultyGraphStatus status, string message)
            : base(message)
        {
            Status = status;
        }
    }
}
#endif