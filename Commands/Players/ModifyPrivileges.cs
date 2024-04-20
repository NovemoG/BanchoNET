using BanchoNET.Attributes;
using BanchoNET.Models;
using BanchoNET.Objects.Privileges;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    [Command("addpriv",
        Privileges.Administrator | Privileges.Developer,
        "Adds given privilege to a player with given username. Syntax: addprivs <username> <privilege>",
        "\nYou can only add privileges that are equal or lower in rank than yours. If player's username contains spaces," +
        "\nplease replace them with underscores." +
        "\nAvailable privileges: alumni, nominator, Moderator, administrator, developer, staff",
        ["ap"])]
    private async Task<string> AddPrivileges(CommandParameters parameters, params string[] args)
    {
        if (args.Length == 0)
            return $"No parameter(s) provided. Use '{_prefix}help addpriv' for more information.";

        var username = args[0];
        var priv = args[1].ToLower();
        
        var player = await players.GetPlayerOrOffline(username);

        if (player == null)
            return "Player not found. Make sure you provided correct username.";

        if (!Enum.TryParse(priv.FirstCharToUpper(), out Privileges privilege))
            return $"Invalid privilege provided. Use '{_prefix}help addpriv' for more information.";
        
        if (parameters.Player.Privileges < privilege)
            return "You can't add a privilege that is higher in rank than yours.";
        
        if (player.Privileges.HasPrivilege(privilege))
            return $"{player.Username} already has this privilege.";
            
        await players.ModifyPlayerPrivileges(player, privilege, false);
        return $"Successfully added privilege to {player.Username}.";
    }

    [Command("rmpriv",
        Privileges.Administrator | Privileges.Developer,
        "Removes given privilege from a player with given username. Syntax: rmpriv <username> <privilege>",
        "\nYou can only remove privileges that are lower in rank than yours. If player's username contains spaces," +
        "\nplease replace them with underscores." +
        "\nAvailable privileges: alumni, nominator, Moderator, administrator, developer, staff",
        ["rp"])]
    private async Task<string> RemovePrivileges(CommandParameters parameters, params string[] args)
    {
        if (args.Length == 0)
            return $"No parameter(s) provided. Use '{_prefix}help rmpriv' for more information.";
        
        var username = args[0];
        var priv = args[1].ToLower();
        
        if (parameters.Player.SafeName == username.MakeSafe())
            return "You can't remove your own privileges.";
        
        var player = await players.GetPlayerOrOffline(username);

        if (player == null)
            return "Player not found. Make sure you provided correct username.";

        if (!Enum.TryParse(priv.FirstCharToUpper(), out Privileges privilege))
            return $"Invalid privilege provided. Use '{_prefix}help addpriv' for more information.";
        
        //TODO both in add and remove for player privileges and given privilege
        if (parameters.Player.Privileges < privilege)
            return $"{player.Username} has a privilege that is higher in rank than yours.";
        
        if (!player.Privileges.HasPrivilege(privilege))
            return $"{player.Username} does not have that privilege.";
            
        await players.ModifyPlayerPrivileges(player, privilege, true);
        return $"Stripped {player.Username} from his privilege.";
    }
}