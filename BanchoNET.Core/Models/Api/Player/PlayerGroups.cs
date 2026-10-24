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

    public static Group[] FromPrivileges(int privileges) => FromPrivileges((PlayerPrivileges)privileges);
    
    public static Group[] FromPrivileges(PlayerPrivileges privileges)
    {
        var group = privileges.GetStaffPrivilege() switch
        {
            PlayerPrivileges.Developer => Developer,
            PlayerPrivileges.Administrator => Administrator,
            PlayerPrivileges.Moderator => Moderator,
            PlayerPrivileges.Nominator => Nominator,
            _ => null,
        };

        return group == null ? [] : [group];
    }
}