using System.Collections.Immutable;
using BanchoNET.Core.Models;

namespace BanchoNET.Core.Utils.Extensions;

public static class ModsExtensions
{
    public static readonly ImmutableDictionary<string, Mods> ModsMap = ImmutableDictionary.CreateRange(new Dictionary<string, Mods>{
        {"nm", Mods.None}, {"nomod", Mods.None},
        {"nf", Mods.NoFail},
        {"ez", Mods.Easy},
        {"hd", Mods.Hidden},
        {"hr", Mods.HardRock},
        {"sd", Mods.SuddenDeath},
        {"dt", Mods.DoubleTime},
        {"rx", Mods.Relax},
        {"ht", Mods.HalfTime},
        {"nc", Mods.NightCore},
        {"fl", Mods.FlashLight},
        {"so", Mods.SpunOut},
        {"ap", Mods.Autopilot},
        {"pf", Mods.Perfect},
        {"k4", Mods.Key4},
        {"k5", Mods.Key5},
        {"k6", Mods.Key6},
        {"k7", Mods.Key7},
        {"k8", Mods.Key8},
        {"fi", Mods.FadeIn},
        {"rd", Mods.Random},
        {"k9", Mods.Key9},
        {"cp", Mods.Coop},
        {"k1", Mods.Key1},
        {"k3", Mods.Key3},
        {"k2", Mods.Key2},
        {"mr", Mods.Mirror},
    });
    
    /// <summary>
    /// Parses a string of shortcut mods (i.e., ezdtnf) into a <see cref="Mods"/> enum
    /// </summary>
    /// <param name="mods">A string value of shortcut mods</param>
    /// <param name="mode">Game mode to account for when parsing mods</param>
    /// <returns>A <see cref="Mods"/> enum value</returns>
    public static Mods ParseMods(this string mods, GameMode mode = GameMode.VanillaStd)
    {
        var splitMods = mods.SplitToParts(2).ToArray();
        
        return ParseMods(splitMods);
    }

    /// <summary>
    /// Parses an array of string mod values (i.e., ez dt nf, easy doubletime nofail, 64 2 1)
    /// into a <see cref="Mods"/> enum
    /// </summary>
    /// <param name="mods">An array of string mod values</param>
    /// <param name="mode">Game mode to account for when parsing mods</param>
    /// <returns>A <see cref="Mods"/> enum value</returns>
    public static Mods ParseMods(this string[] mods, GameMode mode = GameMode.VanillaStd)
    {
        var result = Mods.None;
        
        foreach (var modName in mods)
        {
            if (ModsMap.TryGetValue(modName.ToLower(), out var modMap))
            {
                if (modMap == Mods.None)
                {
                    result = Mods.None;
                    break;
                }
                
                if (mode != GameMode.VanillaMania)
                    if (modMap > Mods.Perfect)
                        continue;

                result |= modMap;
            }
            else if (Enum.TryParse(modName, true, out Mods modParse))
            {
                if (modParse == Mods.None)
                {
                    result = Mods.None;
                    break;
                }

                if (mode != GameMode.VanillaMania)
                    if (modParse > Mods.Perfect)
                        continue;

                result |= modParse;
            }
        }
        
        if ((result & Mods.InvalidMods) != 0)
            result &= ~Mods.InvalidMods;

        return result;
    }
}