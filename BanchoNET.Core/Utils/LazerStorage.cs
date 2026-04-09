namespace BanchoNET.Core.Utils;

public static class LazerStorage
{
    public static readonly string LazerPath = Path.Combine(Storage.LazerPath, "Lazer");
    public static readonly string CurrentLazerVersionFile = Path.Combine(Storage.LazerPath, "LazerVersion.txt");
    
    public static readonly string ReleasesPath = Path.Combine(
        Storage.LazerPath, "Releases", "releases.win.json"
    );
    
    public static string GetReleasesPath(string tagName) => Path.Combine(
        Storage.LazerPath, "Releases", $"releases.{tagName}.json"
    );
    
    public static string GetReleaseFilePath(string fileName) => Path.Combine(
        Storage.LazerPath, "Releases", fileName
    );
    
    public static readonly string ProductionEndpointPath = Path.Combine(
        LazerPath, "osu.Game", "Online", "ProductionEndpointConfiguration.cs"
    );

    public static readonly string TrustedDomainStorePath = Path.Combine(
        LazerPath, "osu.Game", "Online", "TrustedDomainOnlineStore.cs"
    );

    public static readonly string DrawableAvatarPath = Path.Combine(
        LazerPath, "osu.Game", "Users", "Drawables", "DrawableAvatar.cs"
    );
    
    public static readonly string LazerProjectPath = Path.Combine(
        LazerPath, "osu.Desktop"
    );

    public static readonly string LazerPublishPath = Path.Combine(
        LazerProjectPath, "publish"
    );

    public static readonly string VelopackUpdaterPath = Path.Combine(
        LazerPath, "osu.Desktop", "Updater", "VelopackUpdateManager.cs"
    );

    public static readonly string IconPath = Path.Combine(
        LazerPath, "osu.Desktop", "lazer.ico"
    );

    public static readonly string OsuDesktopCsproj = Path.Combine(
        LazerPath, "osu.Desktop", "osu.Desktop.csproj"
    );
}