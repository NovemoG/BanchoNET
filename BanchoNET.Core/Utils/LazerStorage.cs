namespace BanchoNET.Core.Utils;

public static class LazerStorage
{
    public static readonly string LazerPath = Path.Combine(Storage.LazerPath, "Lazer");
    public static readonly string ReleasesPath = Path.Combine(Storage.LazerPath, "Releases");
    private static readonly string OsuGamePath = Path.Combine(LazerPath, "osu.Game");
    
    public static readonly string CurrentLazerVersionFile = Path.Combine(Storage.LazerPath, "LazerVersion.txt");
    
    public static readonly string ReleasesFilePath = Path.Combine(
        ReleasesPath, "releases.win.json"
    );
    
    public static string GetReleasesPath(string tagName) => Path.Combine(
        ReleasesPath, $"releases.{tagName}.json"
    );
    
    public static string GetLazerPortablePath(bool tachyon) => Path.Combine(
        ReleasesPath, $"{AppSettings.LazerName}-{(tachyon ? "tachyon" : "lazer")}-Portable.zip"
    );
    
    public static string GetReleaseFilePath(string fileName) => Path.Combine(
        ReleasesPath, fileName
    );
    
    public static readonly string ProductionEndpointPath = Path.Combine(
        OsuGamePath, "Online", "ProductionEndpointConfiguration.cs"
    );

    public static readonly string TrustedDomainStorePath = Path.Combine(
        OsuGamePath, "Online", "TrustedDomainOnlineStore.cs"
    );

    public static readonly string DrawableAvatarPath = Path.Combine(
        OsuGamePath, "Users", "Drawables", "DrawableAvatar.cs"
    );
    
    public static readonly string LazerProjectPath = Path.Combine(
        LazerPath, "osu.Desktop"
    );

    public static readonly string LazerPublishPath = Path.Combine(
        LazerProjectPath, "publish"
    );

    public static readonly string VelopackUpdaterPath = Path.Combine(
        LazerProjectPath, "Updater", "VelopackUpdateManager.cs"
    );

    public static readonly string IconPath = Path.Combine(
        LazerProjectPath, "lazer.ico"
    );

    public static readonly string OsuDesktopCsproj = Path.Combine(
        LazerProjectPath, "osu.Desktop.csproj"
    );
}