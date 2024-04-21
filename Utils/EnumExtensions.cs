using BanchoNET.Objects;
using BanchoNET.Objects.Beatmaps;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Utils;

public static class EnumExtensions
{
	public static bool HasAnyPrivilege(this Privileges value, Privileges checkFlags)
	{
		return (value & checkFlags) != 0;
	}

	public static Privileges GetHighestPrivilege(this Privileges privilege)
	{
		var value = (uint)privilege;
		var last = value;

		while (value != 0)
		{
			last = value;
			value &= value - 1;
		}

		return (Privileges)last;
	}

	public static bool CompareHighestPrivileges(this Privileges privilege, Privileges privilege2)
	{
		return privilege.GetHighestPrivilege() >= privilege2.GetHighestPrivilege();
	}

	public static bool HasPrivilege(this Privileges value, Privileges privilege)
	{
		return (value & privilege) == privilege;
	}

	public static bool HasPrivilege(this ClientPrivileges value, ClientPrivileges privilege)
	{
		return (value & privilege) == privilege;
	}
	
	public static bool HasMod(this Mods value, Mods mod)
	{
		return (value & mod) == mod;
	}
}