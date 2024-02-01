using BanchoNET.Models;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Utils;

public static class Users
{
	public static bool CheckPassword(this Player player, string password)
	{
		if (string.IsNullOrEmpty(player.PasswordHash)) return false;

		return password == player.PasswordHash;
	}

	public static ClientPrivileges BanchoPrivileges(this Player player)
	{
		var returnPrivileges = 0;

		Console.WriteLine((player.Privileges & (int)Privileges.UNRESTRICTED));
		
		if ((player.Privileges & (int)Privileges.UNRESTRICTED) != 0)
		{
			returnPrivileges |= (int)ClientPrivileges.PLAYER;
		}
		if ((player.Privileges & (int)Privileges.SUPPORTER) != 0)
		{
			returnPrivileges |= (int)ClientPrivileges.SUPPORTER;
		}
		if ((player.Privileges & (int)Privileges.MODERATOR) != 0)
		{
			returnPrivileges |= (int)ClientPrivileges.MODERATOR;
		}
		if ((player.Privileges & (int)Privileges.ADMINISTRATOR) != 0)
		{
			returnPrivileges |= (int)ClientPrivileges.TOURNAMENT;
		}
		if ((player.Privileges & (int)Privileges.DEVELOPER) != 0)
		{
			returnPrivileges |= (int)ClientPrivileges.OWNER;
		}

		return (ClientPrivileges)returnPrivileges;
	}
}