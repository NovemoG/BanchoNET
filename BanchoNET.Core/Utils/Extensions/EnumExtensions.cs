using System.Text;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Mods;
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
	
	private static readonly (LegacyMods Mod, string ShortCode)[] ModMap =
	{
		(LegacyMods.NoFail, "NF"),
		(LegacyMods.Easy, "EZ"),
		(LegacyMods.Hidden, "HD"),
		(LegacyMods.HardRock, "HR"),
		(LegacyMods.SuddenDeath, "SD"),
		(LegacyMods.DoubleTime, "DT"),
		(LegacyMods.Relax, "RX"),
		(LegacyMods.HalfTime, "HT"),
		(LegacyMods.NightCore, "NC"),
		(LegacyMods.FlashLight, "FL"),
		(LegacyMods.SpunOut, "SO"),
		(LegacyMods.Autopilot, "AP"),
		(LegacyMods.Perfect, "PF"),
		(LegacyMods.Key4, "4K"),
		(LegacyMods.Key5, "5K"),
		(LegacyMods.Key6, "6K"),
		(LegacyMods.Key7, "7K"),
		(LegacyMods.Key8, "8K"),
		(LegacyMods.FadeIn, "FI"),
		(LegacyMods.Random, "RD"),
		(LegacyMods.Key9, "9K"),
		(LegacyMods.Coop, "CO"),
		(LegacyMods.Key1, "1K"),
		(LegacyMods.Key3, "3K"),
		(LegacyMods.Key2, "2K"),
		(LegacyMods.ScoreV2, "SV2")
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
	
	public static bool HasMod(this LegacyMods value, LegacyMods mod)
	{
		return (value & mod) == mod;
	}
	
	public static string ToShortString(this LegacyMods mods)
	{
		var sb = new StringBuilder();
		
		foreach (var (mod, code) in ModMap)
			if (mods.HasMod(mod))
				sb.Append(code);

		return sb.ToString();
	}
	
	public static List<ApiMod> ToLazerMods(
		this LegacyMods legacyMods
	) {
		var mods = new List<ApiMod>{ new(){ Acronym = "CL" } };

		foreach (var (mod, code) in ModMap)
			if (legacyMods.HasMod(mod))
				mods.Add(new ApiMod{ Acronym = code });

		return mods;
	}
}