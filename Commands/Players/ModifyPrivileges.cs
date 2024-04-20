﻿using BanchoNET.Attributes;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("addpriv",
        Privileges.Administrator | Privileges.Developer,
        "Adds given privilege to a player with given username. Syntax: addpriv <username> <privilege>",
        "\nYou can only add privileges that are lower in rank than yours. If player's username contains spaces" +
        "\nplease replace them with underscores." +
        "\nAvailable privileges: nominator, moderator, administrator, developer.",
        ["ap"])]
    private async Task<string> AddPrivileges(params string[] args)
    {
        if (args.Length == 0)
            return $"No parameter(s) provided. Syntax: {_prefix}addpriv <username> <privilege>.";
        
        if (args.Length == 1)
            return "No privilege provided. Available privileges: nominator, moderator, administrator, developer.";

        var username = args[0];
        var priv = args[1].ToLower();
        
        if (!Enum.TryParse(priv.FirstCharToUpper(), out Privileges privilege))
            return "Invalid privilege provided. Available privileges: nominator, moderator, administrator, developer.";
        
        if (_playerCtx.Privileges.GetHighestPrivilege() < privilege)
            return "You can't add a privilege that is higher in rank than yours.";
        
        var player = await players.GetPlayerOrOffline(username);

        if (player == null)
            return "Player not found. Make sure you provided correct username.";
        
        if (player.Privileges.HasPrivilege(privilege))
            return $"{player.Username} already has this privilege.";
            
        await players.ModifyPlayerPrivileges(player, privilege, false);
        return $"Successfully added privilege to {player.Username}.";
    }

    [Command("rmpriv",
        Privileges.Administrator | Privileges.Developer,
        "Removes given privilege from a player with given username. Syntax: rmpriv <username> <privilege>",
        "\nYou can only remove privileges that are lower in rank than yours. If player's username contains spaces" +
        "\nplease replace them with underscores." +
        "\nAvailable privileges: nominator, moderator, administrator, developer.",
        ["rp"])]
    private async Task<string> RemovePrivileges(params string[] args)
    {
        if (args.Length == 0)
            return $"No parameter(s) provided. Use '{_prefix}help rmpriv' for more information.";
        
        if (args.Length == 1)
            return "No privilege provided. Available privileges: nominator, moderator, administrator, developer.";
        
        var username = args[0];
        var priv = args[1].ToLower();
        
        if (_playerCtx.SafeName == username.MakeSafe())
            return "You can't remove your own privileges.";
        
        if (!Enum.TryParse(priv.FirstCharToUpper(), out Privileges privilege))
            return $"Invalid privilege provided. Use '{_prefix}help addpriv' for more information.";
        
        if (_playerCtx.Privileges.GetHighestPrivilege() <= privilege)
            return "You can't remove a privilege that is higher or equal in rank.";
        
        var player = await players.GetPlayerOrOffline(username);

        if (player == null)
            return "Player not found. Make sure you provided correct username.";
        
        if (_playerCtx.Privileges.CompareHighestPrivileges(player.Privileges))
            return $"{player.Username} has a privilege that is higher or equal in rank.";
        
        if (!player.Privileges.HasPrivilege(privilege))
            return $"{player.Username} does not have that privilege.";
            
        await players.ModifyPlayerPrivileges(player, privilege, true);
        return $"Stripped {player.Username} from his privilege.";
    }
}