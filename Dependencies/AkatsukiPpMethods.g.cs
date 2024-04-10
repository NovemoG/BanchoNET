#pragma warning disable CS8500
#pragma warning disable CS8981
using System;
using System.Runtime.InteropServices;
using BanchoNET.Objects;
using BanchoNET.Objects.Scores;
using BanchoNET.Utils;

namespace AkatsukiPp
{
    public static partial class AkatsukiPpMethods
    {
        const string __DllName = "akatsuki_pp_cs";
        
        [DllImport(__DllName, EntryPoint = "ComputePp", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern double ComputePp(string path, byte mode, uint mods, nuint combo, float acc, nuint n300, nuint n_geki, nuint n100, nuint n_katu, nuint n50, nuint n_misses);
        
        public static float ComputeScorePp(string osuFilePath, Score score)
        {
            return (float)ComputePp(
                osuFilePath,
                (byte)score.Mode.AsVanilla(),
                (uint)score.Mods,
                new UIntPtr((uint)score.MaxCombo),
                score.Acc,
                new UIntPtr((uint)score.Count300),
                new UIntPtr((uint)score.Gekis),
                new UIntPtr((uint)score.Count100),
                new UIntPtr((uint)score.Katus),
                new UIntPtr((uint)score.Count50),
                new UIntPtr((uint)score.Misses)
            );
        }

        public static float ComputeNoMissesScorePp(string osuFilePath, Score score, int maxCombo)
        {
            var acc = ScoreExtensions.CalculateAccuracy(
                score.Mode,
                score.Mods,
                score.Count300 + score.Misses,
                score.Count100,
                score.Count50,
                0,
                score.Gekis,
                score.Katus);
            
            return (float)ComputePp(
                osuFilePath,
                (byte)score.Mode.AsVanilla(),
                (uint)score.Mods,
                new UIntPtr((uint)maxCombo),
                acc,
                new UIntPtr((uint)(score.Count300 + score.Misses)),
                new UIntPtr((uint)score.Gekis),
                new UIntPtr((uint)score.Count100),
                new UIntPtr((uint)score.Katus),
                new UIntPtr((uint)score.Count50),
                new UIntPtr((uint)0)
            );
        }
    }
}
