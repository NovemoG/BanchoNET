namespace BanchoNET.Core.Utils;

public static class LazerStorage
{
    private static readonly string LazerPath = Path.Combine(Storage.LazerPath, "Lazer");
    public static readonly string CurrentLazerVersionFile = Path.Combine(Storage.LazerPath, "LazerVersion.txt");

    public static string GetReleasePath(string tagName) {
        var path = Path.Combine(Storage.LazerPath, tagName);
        Directory.CreateDirectory(path);
        return path;
    }
    
    public static string GetReleasesPath(string tagName) => Path.Combine(
        LazerPath, tagName, "Releases", "releases.win.json"
    );
    
    public static string GetReleaseFilePath(string tagName, string fileName) => Path.Combine(
        LazerPath, tagName, "Releases", fileName
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