namespace BanchoNET.Core.Models.Api.Player;

public class BasicApiPlayer
{
    public string AvatarUrl => $"https://a.ppy.sh/{Id}.jpg";
    public string CountryCode { get; set; } = "Unknown";
    public string DefaultGroup { get; set; } = "default"; //TODO
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public bool IsBot { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOnline { get; set; }
    public bool IsSupporter { get; set; }
    public DateTimeOffset? LastVisit { get; set; }
    public bool PmFriendsOnly { get; set; }
    public string? ProfileColour { get; set; } //TODO
    public string Username { get; set; } = null!;
    public Country Country { get; set; } = null!;
    public Cover Cover { get; set; } = new(); //TODO
    public Team? Team { get; set; }
}