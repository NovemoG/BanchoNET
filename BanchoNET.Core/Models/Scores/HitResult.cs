using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Scores;

public enum HitResult
{
    [EnumMember(Value = "none")]
    [JsonStringEnumMemberName("none")]
    None,

    [EnumMember(Value = "miss")]
    [JsonStringEnumMemberName("miss")]
    Miss,

    [EnumMember(Value = "meh")]
    [JsonStringEnumMemberName("meh")]
    Meh,

    [EnumMember(Value = "ok")]
    [JsonStringEnumMemberName("ok")]
    Ok,

    [EnumMember(Value = "good")]
    [JsonStringEnumMemberName("good")]
    Good,

    [EnumMember(Value = "great")]
    [JsonStringEnumMemberName("great")]
    Great,

    [EnumMember(Value = "perfect")]
    [JsonStringEnumMemberName("perfect")]
    Perfect,

    [EnumMember(Value = "small_tick_miss")]
    [JsonStringEnumMemberName("small_tick_miss")]
    SmallTickMiss,

    [EnumMember(Value = "small_tick_hit")]
    [JsonStringEnumMemberName("small_tick_hit")]
    SmallTickHit,

    [EnumMember(Value = "large_tick_miss")]
    [JsonStringEnumMemberName("large_tick_miss")]
    LargeTickMiss,

    [EnumMember(Value = "large_tick_hit")]
    [JsonStringEnumMemberName("large_tick_hit")]
    LargeTickHit,

    [EnumMember(Value = "small_bonus")]
    [JsonStringEnumMemberName("small_bonus")]
    SmallBonus,

    [EnumMember(Value = "large_bonus")]
    [JsonStringEnumMemberName("large_bonus")]
    LargeBonus,

    [EnumMember(Value = "ignore_miss")]
    [JsonStringEnumMemberName("ignore_miss")]
    IgnoreMiss,

    [EnumMember(Value = "ignore_hit")]
    [JsonStringEnumMemberName("ignore_hit")]
    IgnoreHit,

    [EnumMember(Value = "combo_break")]
    [JsonStringEnumMemberName("combo_break")]
    ComboBreak,

    [EnumMember(Value = "slider_tail_hit")]
    [JsonStringEnumMemberName("slider_tail_hit")]
    SliderTailHit,

    [EnumMember(Value = "legacy_combo_increase")]
    [JsonStringEnumMemberName("legacy_combo_increase")]
    LegacyComboIncrease = 99
}