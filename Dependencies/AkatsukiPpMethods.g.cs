#pragma warning disable CS8500
#pragma warning disable CS8981
using System;
using System.Runtime.InteropServices;

namespace AkatsukiPp
{
    public static partial class AkatsukiPpMethods
    {
        const string __DllName = "akatsuki_pp_cs";
        
        [DllImport(__DllName, EntryPoint = "ComputePp", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double ComputePp(string path, byte mode, uint mods, nuint combo, float acc, nuint n300, nuint n_geki, nuint n100, nuint n_katu, nuint n50, nuint n_misses);
    }
}
