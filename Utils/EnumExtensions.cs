using BanchoNET.Objects;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Utils;

public static class EnumExtensions
{
	public static bool HasPrivileges(this Privileges value, params Privileges[] checkFlags)
	{
		return checkFlags.All(flag => (value & flag) == flag);
	}

	public static bool HasPrivilege(this Privileges value, Privileges privilege)
	{
		return (value & privilege) == privilege;
	}
	
	public static bool HasPrivileges(this ClientPrivileges value, params ClientPrivileges[] checkFlags)
	{
		return checkFlags.All(flag => (value & flag) == flag);
	}

	public static bool HasPrivilege(this ClientPrivileges value, ClientPrivileges privilege)
	{
		return (value & privilege) == privilege;
	}
	
	public static bool HasMods(this Mods value, params Mods[] checkFlags)
	{
		return checkFlags.All(flag => (value & flag) == flag);
	}
	
	public static bool HasMod(this Mods value, Mods mod)
	{
		return (value & mod) == mod;
	}
}