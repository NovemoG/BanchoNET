using System.Collections.Immutable;
using BanchoNET.Core.Models;

namespace BanchoNET.Core.Utils.Extensions;

public static class ModsExtensions
{
    public static readonly ImmutableDictionary<string, StableMods> ModsMap = ImmutableDictionary.CreateRange(new Dictionary<string, StableMods>{
        {"nm", StableMods.None}, {"nomod", StableMods.None},
        {"nf", StableMods.NoFail},
        {"ez", StableMods.Easy},
        {"hd", StableMods.Hidden},
        {"hr", StableMods.HardRock},
        {"sd", StableMods.SuddenDeath},
        {"dt", StableMods.DoubleTime},
        {"rx", StableMods.Relax},
        {"ht", StableMods.HalfTime},
        {"nc", StableMods.NightCore},
        {"fl", StableMods.FlashLight},
        {"so", StableMods.SpunOut},
        {"ap", StableMods.Autopilot},
        {"pf", StableMods.Perfect},
        {"k4", StableMods.Key4},
        {"k5", StableMods.Key5},
        {"k6", StableMods.Key6},
        {"k7", StableMods.Key7},
        {"k8", StableMods.Key8},
        {"fi", StableMods.FadeIn},
        {"rd", StableMods.Random},
        {"k9", StableMods.Key9},
        {"cp", StableMods.Coop},
        {"k1", StableMods.Key1},
        {"k3", StableMods.Key3},
        {"k2", StableMods.Key2},
        {"mr", StableMods.Mirror},
    });
    
    /// <summary>
    /// Parses a string of shortcut mods (i.e., ezdtnf) into a <see cref="StableMods"/> enum
    /// </summary>
    /// <param name="mods">A string value of shortcut mods</param>
    /// <param name="mode">Game mode to account for when parsing mods</param>
    /// <returns>A <see cref="StableMods"/> enum value</returns>
    public static StableMods ParseMods(this string mods, GameMode mode = GameMode.VanillaStd)
    {
        var splitMods = mods.SplitToParts(2).ToArray();
        
        return ParseMods(splitMods);
    }

    /// <summary>
    /// Parses an array of string mod values (i.e., ez dt nf, easy doubletime nofail, 64 2 1)
    /// into a <see cref="StableMods"/> enum
    /// </summary>
    /// <param name="mods">An array of string mod values</param>
    /// <param name="mode">Game mode to account for when parsing mods</param>
    /// <returns>A <see cref="StableMods"/> enum value</returns>
    public static StableMods ParseMods(this string[] mods, GameMode mode = GameMode.VanillaStd)
    {
        var result = StableMods.None;
        
        foreach (var modName in mods)
        {
            if (ModsMap.TryGetValue(modName.ToLower(), out var modMap))
            {
                if (modMap == StableMods.None)
                {
                    result = StableMods.None;
                    break;
                }
                
                if (mode != GameMode.VanillaMania)
                    if (modMap > StableMods.Perfect)
                        continue;

                result |= modMap;
            }
            else if (Enum.TryParse(modName, true, out StableMods modParse))
            {
                if (modParse == StableMods.None)
                {
                    result = StableMods.None;
                    break;
                }

                if (mode != GameMode.VanillaMania)
                    if (modParse > StableMods.Perfect)
                        continue;

                result |= modParse;
            }
        }
        
        if ((result & StableMods.InvalidMods) != 0)
            result &= ~StableMods.InvalidMods;

        return result;
    }
}