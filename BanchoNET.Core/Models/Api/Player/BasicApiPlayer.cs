using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Player;

public class BasicApiPlayer
{
    public string AvatarUrl => $"https://a.{AppSettings.Domain}/{Id}";
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
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Country? Country { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Cover? Cover { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Team? Team { get; set; }
    
    [JsonConstructor]
    public BasicApiPlayer() { }

    public BasicApiPlayer(
        Players.Player player
    ) {
        CountryCode = player.CountryCode.ToString();
        Id = player.Id;
        //TODO IsActive
        IsBot = player.IsBot;
        //TODO IsOnline
        IsSupporter = player.IsSupporter;
        LastVisit = player.AppearOffline ? null : player.LoginTime;
        PmFriendsOnly = player.PmFriendsOnly;
        Username = player.Username;
        Country = CountryCode.ParseCountry();
        //TODO Cover
        Cover = new Cover
        {
            CustomUrl = $"https://assets.{AppSettings.Domain}/user-profile-covers/{Id}",
            Url = $"https://assets.{AppSettings.Domain}/user-profile-covers/{Id}",
        };
        //TODO Team
    }

    public BasicApiPlayer(
        PlayerDto playerDto
    ) {
        CountryCode = playerDto.Country;
        Id = playerDto.Id;
        //TODO IsActive
        //TODO IsBot
        //TODO IsOnline
        IsSupporter = playerDto.IsSupporter;
        LastVisit = playerDto.HideOnlineActivity ? null : playerDto.LastLoginTime;
        PmFriendsOnly = playerDto.PmFriendsOnly;
        Username = playerDto.Username;
        Country = CountryCode.ParseCountry();
        //TODO Cover
        Cover = new Cover
        {
            CustomUrl = $"https://assets.{AppSettings.Domain}/user-profile-covers/{Id}",
            Url = $"https://assets.{AppSettings.Domain}/user-profile-covers/{Id}",
        };
        //TODO Team
    }
}