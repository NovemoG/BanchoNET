using System.Collections.Immutable;
using BanchoNET.Objects;

namespace BanchoNET.Utils;

public static class ModsMap
{
    public static readonly ImmutableDictionary<string, Mods> Map = ImmutableDictionary.CreateRange(new Dictionary<string, Mods>{
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
}