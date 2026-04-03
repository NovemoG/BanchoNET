using System.Runtime.Serialization;

namespace BanchoNET.Core.Models.Scores;

public enum HitResult
{
    [EnumMember(Value = "none")]
    None,

    [EnumMember(Value = "miss")]
    Miss,

    [EnumMember(Value = "meh")]
    Meh,

    [EnumMember(Value = "ok")]
    Ok,

    [EnumMember(Value = "good")]
    Good,

    [EnumMember(Value = "great")]
    Great,

    [EnumMember(Value = "perfect")]
    Perfect,

    [EnumMember(Value = "small_tick_miss")]
    SmallTickMiss,

    [EnumMember(Value = "small_tick_hit")]
    SmallTickHit,

    [EnumMember(Value = "large_tick_miss")]
    LargeTickMiss,

    [EnumMember(Value = "large_tick_hit")]
    LargeTickHit,

    [EnumMember(Value = "small_bonus")]
    SmallBonus,

    [EnumMember(Value = "large_bonus")]
    LargeBonus,

    [EnumMember(Value = "ignore_miss")]
    IgnoreMiss,

    [EnumMember(Value = "ignore_hit")]
    IgnoreHit,

    [EnumMember(Value = "combo_break")]
    ComboBreak,

    [EnumMember(Value = "slider_tail_hit")]
    SliderTailHit,

    [EnumMember(Value = "legacy_combo_increase")]
    LegacyComboIncrease = 99
}