using System.Text;
using BanchoNET.Objects;
using BanchoNET.Objects.Privileges;

namespace BanchoNET.Utils.Extensions;

public static class EnumExtensions
{
	private static readonly (Mods Mod, string ShortCode)[] ModMap =
	{
		(Mods.NoFail, "NF"),
		(Mods.Easy, "EZ"),
		(Mods.Hidden, "HD"),
		(Mods.HardRock, "HR"),
		(Mods.SuddenDeath, "SD"),
		(Mods.DoubleTime, "DT"),
		(Mods.Relax, "RX"),
		(Mods.HalfTime, "HT"),
		(Mods.NightCore, "NC"),
		(Mods.FlashLight, "FL"),
		(Mods.SpunOut, "SO"),
		(Mods.Autopilot, "AP"),
		(Mods.Perfect, "PF"),
		(Mods.Key4, "4K"),
		(Mods.Key5, "5K"),
		(Mods.Key6, "6K"),
		(Mods.Key7, "7K"),
		(Mods.Key8, "8K"),
		(Mods.FadeIn, "FI"),
		(Mods.Random, "RD"),
		(Mods.Key9, "9K"),
		(Mods.Coop, "CO"),
		(Mods.Key1, "1K"),
		(Mods.Key3, "3K"),
		(Mods.Key2, "2K"),
		(Mods.ScoreV2, "SV2")
	};
	
	public static bool HasAnyPrivilege(this PlayerPrivileges value, PlayerPrivileges checkFlags)
	{
		return (value & checkFlags) != 0;
	}

	public static PlayerPrivileges GetHighestPrivilege(this PlayerPrivileges privilege)
	{
		var value = (uint)privilege;
		var last = value;

		while (value != 0)
		{
			last = value;
			value &= value - 1;
		}

		return (PlayerPrivileges)last;
	}
	
	public static ClientPrivileges GetHighestPrivilege(this ClientPrivileges privilege)
	{
		var value = (uint)privilege;
		var last = value;

		while (value != 0)
		{
			last = value;
			value &= value - 1;
		}

		return (ClientPrivileges)last;
	}

	public static bool CompareHighestPrivileges(this PlayerPrivileges privilege, PlayerPrivileges privilege2)
	{
		return privilege.GetHighestPrivilege() >= privilege2.GetHighestPrivilege();
	}
	
	public static bool CompareHighestPrivileges(this ClientPrivileges privilege, ClientPrivileges privilege2)
	{
		return privilege.GetHighestPrivilege() >= privilege2.GetHighestPrivilege();
	}

	public static bool HasPrivilege(this PlayerPrivileges value, PlayerPrivileges privilege)
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
	
	public static string ToShortString(this Mods mods)
	{
		var sb = new StringBuilder();
		
		foreach (var (mod, code) in ModMap)
			if (mods.HasMod(mod))
				sb.Append(code);

		return sb.ToString();
	}
}