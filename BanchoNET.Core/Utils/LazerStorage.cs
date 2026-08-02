using BanchoNET.Core.Models;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Utils;

public static class LazerStorage
{
    public static readonly string LazerPath = Path.Combine(Storage.LazerPath, "Lazer");
    
    public static readonly string ReleasesPath = Path.Combine(Storage.LazerPath, "Releases");
    
    public static readonly string WinPackPath = Path.Combine(Storage.LazerPath, "WinPack");
    public static readonly string LinuxPackPath = Path.Combine(Storage.LazerPath, "LinuxPack");

    private static readonly string OsuGamePath = Path.Combine(LazerPath, "osu.Game");
    
    public static readonly string CurrentLazerVersionFile = Path.Combine(Storage.LazerPath, "LazerVersion.txt");
    
    public static string GetPackPath(LazerPlatform platform) => platform switch
    {
        LazerPlatform.Win => WinPackPath,
        LazerPlatform.Linux => LinuxPackPath,
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    };

    public static string GetGeneratedReleasesFilePath(LazerPlatform platform) => Path.Combine(
        GetPackPath(platform), "Releases", $"releases.{platform.ToFileName()}.json"
    );

    public static string GetReleasesPath(string tagName, LazerPlatform platform = LazerPlatform.Win) => Path.Combine(
        ReleasesPath, $"releases.{tagName}.{platform.ToFileName()}.json"
    );

    public static string GetGeneratedPortablePath(LazerPlatform platform) => Path.Combine(
        GetPackPath(platform), "Releases",
        platform switch {
            LazerPlatform.Win => $"{AppSettings.LazerName}-{platform.ToFileName()}-Portable.{PortableExtension(platform)}",
            LazerPlatform.Linux => $"{AppSettings.LazerName}.AppImage",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    });

    public static string GetLazerPortablePath(bool tachyon, LazerPlatform platform = LazerPlatform.Win) => Path.Combine(
        ReleasesPath,
        platform switch {
            LazerPlatform.Win => $"{AppSettings.LazerName}-{(tachyon ? "tachyon" : "lazer")}-{platform.ToFileName()}-Portable.{PortableExtension(platform)}",
            LazerPlatform.Linux => $"{AppSettings.LazerName}-{(tachyon ? "tachyon" : "lazer")}.AppImage",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    });

    public static string GetReleaseFilePath(string tagName, string type, LazerPlatform platform = LazerPlatform.Win) =>
        platform == LazerPlatform.Win
            ? Path.Combine(ReleasesPath, $"{AppSettings.LazerName}-{tagName}-{type}.nupkg")
            : Path.Combine(ReleasesPath, $"{AppSettings.LazerName}-{tagName}-{platform.ToFileName()}-{type}.nupkg");

    private static string PortableExtension(LazerPlatform platform) => platform switch
    {
        LazerPlatform.Win => "zip",
        LazerPlatform.Linux => "tar.gz",
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    };
    
    private static readonly string OsuDesktopPath = Path.Combine(LazerPath, "osu.Desktop");

    public static readonly string LazerProjectPath = OsuDesktopPath;

    public static string GetLazerPublishPath(LazerPlatform platform) =>
        Path.Combine(OsuDesktopPath, $"publish-{platform.ToFileName()}");

    public static readonly string VelopackUpdaterPath = Path.Combine(
        OsuDesktopPath, "Updater", "VelopackUpdateManager.cs"
    );
    
    public static readonly string IconPath = Path.Combine(OsuDesktopPath, "lazer.ico");

    public static readonly string OsuDesktopCsproj = Path.Combine(OsuDesktopPath, "osu.Desktop.csproj");

    public static readonly string ProductionEndpointPath = Path.Combine(
        OsuGamePath, "Online", "ProductionEndpointConfiguration.cs"
    );

    public static readonly string TrustedDomainStorePath = Path.Combine(
        OsuGamePath, "Online", "TrustedDomainOnlineStore.cs"
    );

    public static readonly string DrawableAvatarPath = Path.Combine(
        OsuGamePath, "Users", "Drawables", "DrawableAvatar.cs"
    );
}