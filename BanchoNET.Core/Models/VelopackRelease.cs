using BanchoNET.Core.Utils;

namespace BanchoNET.Core.Models;

public class VelopackReleaseFeed
{
    public List<VelopackAsset> Assets { get; set; } = [];
}

public class VelopackAsset
{
    public string PackageId = AppSettings.LazerName;
    public string Version { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SHA1 { get; set; } = string.Empty;
    public string SHA256 { get; set; } = string.Empty;
    public long Size { get; set; }
}