using BanchoNET.Core.Models;

namespace BanchoNET.Core.Utils.Extensions;

public static class LazerPlatformExtensions
{
    public static string ToFileName(this LazerPlatform platform) => platform switch
    {
        LazerPlatform.Win => "win",
        LazerPlatform.Linux => "linux",
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    };
}
