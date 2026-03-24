using System.Collections.Immutable;
using System.Text.Json;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Mods;

namespace BanchoNET.Core.Utils.Extensions;

public static class ModsExtensions
{
    public static readonly ImmutableDictionary<string, LegacyMods> ModsMap = ImmutableDictionary.CreateRange(new Dictionary<string, LegacyMods>{
        {"nm", LegacyMods.None}, {"nomod", LegacyMods.None},
        {"nf", LegacyMods.NoFail},
        {"ez", LegacyMods.Easy},
        {"hd", LegacyMods.Hidden},
        {"hr", LegacyMods.HardRock},
        {"sd", LegacyMods.SuddenDeath},
        {"dt", LegacyMods.DoubleTime},
        {"rx", LegacyMods.Relax},
        {"ht", LegacyMods.HalfTime},
        {"nc", LegacyMods.NightCore},
        {"fl", LegacyMods.FlashLight},
        {"so", LegacyMods.SpunOut},
        {"ap", LegacyMods.Autopilot},
        {"pf", LegacyMods.Perfect},
        {"k4", LegacyMods.Key4},
        {"k5", LegacyMods.Key5},
        {"k6", LegacyMods.Key6},
        {"k7", LegacyMods.Key7},
        {"k8", LegacyMods.Key8},
        {"fi", LegacyMods.FadeIn},
        {"rd", LegacyMods.Random},
        {"k9", LegacyMods.Key9},
        {"cp", LegacyMods.Coop},
        {"k1", LegacyMods.Key1},
        {"k3", LegacyMods.Key3},
        {"k2", LegacyMods.Key2},
        {"mr", LegacyMods.Mirror},
    });
    
    /// <summary>
    /// Parses a string of shortcut mods (i.e., ezdtnf) into a <see cref="LegacyMods"/> enum
    /// </summary>
    /// <param name="mods">A string value of shortcut mods</param>
    /// <param name="mode">Game mode to account for when parsing mods</param>
    /// <returns>A <see cref="LegacyMods"/> enum value</returns>
    public static LegacyMods ParseMods(this string mods, GameMode mode = GameMode.VanillaStd)
    {
        var splitMods = mods.SplitToParts(2).ToArray();
        
        return ParseMods(splitMods, mode);
    }

    /// <summary>
    /// Parses an array of string mod values (i.e., ez dt nf, easy doubletime nofail, 64 2 1)
    /// into a <see cref="LegacyMods"/> enum
    /// </summary>
    /// <param name="mods">An array of string mod values</param>
    /// <param name="mode">Game mode to account for when parsing mods</param>
    /// <returns>A <see cref="LegacyMods"/> enum value</returns>
    public static LegacyMods ParseMods(this string[] mods, GameMode mode = GameMode.VanillaStd)
    {
        var result = LegacyMods.None;
        
        foreach (var modName in mods)
        {
            if (ModsMap.TryGetValue(modName.ToLower(), out var modMap))
            {
                if (modMap == LegacyMods.None)
                {
                    result = LegacyMods.None;
                    break;
                }
                
                if (mode != GameMode.VanillaMania)
                    if (modMap > LegacyMods.Perfect)
                        continue;

                result |= modMap;
            }
            else if (Enum.TryParse(modName, true, out LegacyMods modParse))
            {
                if (modParse == LegacyMods.None)
                {
                    result = LegacyMods.None;
                    break;
                }

                if (mode != GameMode.VanillaMania)
                    if (modParse > LegacyMods.Perfect)
                        continue;

                result |= modParse;
            }
        }
        
        if ((result & LegacyMods.InvalidMods) != 0)
            result &= ~LegacyMods.InvalidMods;

        return result;
    }
    
    public static string ModsToString(
        this ApiScore score
    ) {
        return score.Mods.Aggregate(string.Empty, (current, mod) => current + mod);
    }

    public static List<ApiMod> ToMods(
        this string mods
    ) {
        return string.IsNullOrWhiteSpace(mods)
            ? []
            : mods.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(mod => new ApiMod(mod))
                .ToList();
    }

    public static LegacyMods ToLegacyMods(
        this List<ApiMod> mods
    ) {
        var legacyMods = LegacyMods.None;
        
        foreach (var mod in mods)
            if (ModsMap.TryGetValue(mod.Acronym.ToLower(), out var legacyMod))
                legacyMods |= legacyMod;
        
        return legacyMods;
    }

    public static float GetFloat(
        this object rawValue
    ) {
        if (rawValue is JsonElement { ValueKind: JsonValueKind.Number } element)
            return element.GetSingle();

        throw new ArgumentException("Value must be a number", nameof(rawValue));
    }

    public static double GetDouble(
        this object rawValue
    ) {
        if (rawValue is JsonElement { ValueKind: JsonValueKind.Number } element)
            return element.GetDouble();

        throw new ArgumentException("Value must be a number", nameof(rawValue));
    }
}