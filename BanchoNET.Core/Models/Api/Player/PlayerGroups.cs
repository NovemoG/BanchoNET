using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Core.Models.Api.Player;

public static class PlayerGroups
{
    public static readonly Group Developer = new()
    {
        Id = 11,
        Identifier = "dev",
        Name = "osu! Developers",
        ShortName = "DEV",
        Colour = "#E45678",
        HasListing = true,
    };

    public static readonly Group Administrator = new()
    {
        Id = 33,
        Identifier = "admin",
        Name = "Administrators",
        ShortName = "ADMIN",
        Colour = "#FF5F5F",
    };

    public static readonly Group Moderator = new()
    {
        Id = 4,
        Identifier = "gmt",
        Name = "Global Moderation Team",
        ShortName = "GMT",
        Colour = "#99EB47",
        HasListing = true,
    };

    public static readonly Group Nominator = new()
    {
        Id = 28,
        Identifier = "bng",
        Name = "Beatmap Nominators",
        ShortName = "BN",
        Colour = "#A347EB",
        HasListing = true,
    };
    
    public static readonly Group Staff = new()
    {
        Id = 1001,
        Identifier = "staff",
        Name = "Server Staff",
        ShortName = "STAFF",
        Colour = "#66CCFF",
        HasListing = true,
    };

    public static readonly Group Owner = new()
    {
        Id = 1000,
        Identifier = "owner",
        Name = "Server Owner",
        ShortName = "OWNER",
        Colour = "#FFCC22",
    };

    public static Group[] FromPrivileges(int privileges) => FromPrivileges((PlayerPrivileges)privileges);
    
    public static Group[] FromPrivileges(PlayerPrivileges privileges)
    {
        var staffGroup = (privileges & PlayerPrivileges.Ranks & ~PlayerPrivileges.Owner).GetHighestPrivilege() switch
        {
            PlayerPrivileges.Staff => Staff,
            PlayerPrivileges.Developer => Developer,
            PlayerPrivileges.Administrator => Administrator,
            PlayerPrivileges.Moderator => Moderator,
            PlayerPrivileges.Nominator => Nominator,
            _ => null,
        };

        var groups = new List<Group>();

        if (privileges.HasPrivilege(PlayerPrivileges.Owner))
            groups.Add(Owner);

        if (staffGroup != null)
            groups.Add(staffGroup);

        return groups.ToArray();
    }
}