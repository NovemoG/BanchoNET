using BanchoNET.Objects.Privileges;

namespace BanchoNET.Utils;

public static class EnumExtensions
{
	public static IEnumerable<T> GetValues<T>() where T : Enum
	{
		return (T[])Enum.GetValues(typeof(T));
	}
	
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
}