using System.Text;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using EnumsNET;

namespace BanchoNET.Core.Utils.Extensions;

public static class EnumExtensions
{
	static EnumExtensions() {
		var codes = Enum.GetValues<CountryCode>();
		var dict = new Dictionary<CountryCode, string>();

		foreach (var code in codes)
		{
			dict[code] = code.AsString(EnumFormat.Description) ?? "Unknown";
		}
		
		CountryCodeMap = dict;
	}

	public static readonly IReadOnlyDictionary<CountryCode, string> CountryCodeMap;
	
	public static readonly Dictionary<string, GameMode> ToModeMap = new()
	{
		{ "osu", GameMode.VanillaStd },
		{ "taiko", GameMode.VanillaTaiko },
		{ "mania", GameMode.VanillaMania },
		{ "fruits", GameMode.VanillaCatch }
	};
    
	public static readonly Dictionary<GameMode, string> FromModeMap = new()
	{
		{ GameMode.VanillaStd, "osu" },
		{ GameMode.VanillaTaiko, "taiko" },
		{ GameMode.VanillaMania, "mania" },
		{ GameMode.VanillaCatch, "fruits" }
	};
	
	private static readonly (StableMods Mod, string ShortCode)[] ModMap =
	{
		(StableMods.NoFail, "NF"),
		(StableMods.Easy, "EZ"),
		(StableMods.Hidden, "HD"),
		(StableMods.HardRock, "HR"),
		(StableMods.SuddenDeath, "SD"),
		(StableMods.DoubleTime, "DT"),
		(StableMods.Relax, "RX"),
		(StableMods.HalfTime, "HT"),
		(StableMods.NightCore, "NC"),
		(StableMods.FlashLight, "FL"),
		(StableMods.SpunOut, "SO"),
		(StableMods.Autopilot, "AP"),
		(StableMods.Perfect, "PF"),
		(StableMods.Key4, "4K"),
		(StableMods.Key5, "5K"),
		(StableMods.Key6, "6K"),
		(StableMods.Key7, "7K"),
		(StableMods.Key8, "8K"),
		(StableMods.FadeIn, "FI"),
		(StableMods.Random, "RD"),
		(StableMods.Key9, "9K"),
		(StableMods.Coop, "CO"),
		(StableMods.Key1, "1K"),
		(StableMods.Key3, "3K"),
		(StableMods.Key2, "2K"),
		(StableMods.ScoreV2, "SV2")
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
	
	public static bool HasMod(this StableMods value, StableMods mod)
	{
		return (value & mod) == mod;
	}
	
	public static string ToShortString(this StableMods mods)
	{
		var sb = new StringBuilder();
		
		foreach (var (mod, code) in ModMap)
			if (mods.HasMod(mod))
				sb.Append(code);

		return sb.ToString();
	}
}