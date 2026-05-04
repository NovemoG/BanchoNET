using System.Text.Json;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Scores;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BanchoNET.Core.Utils.Json;

public static class DatabaseDictionaryConverter
{
    public static readonly ValueConverter<Dictionary<HitResult, int>, string> StatisticsConverter = new(
        v => JsonSerializer.Serialize(v),
        v => JsonSerializer.Deserialize<Dictionary<HitResult, int>>(v)!
    );
    
    public static readonly ValueConverter<Dictionary<SkillType, float>, string> SkillsConverter = new(
        v => JsonSerializer.Serialize(v),
        v => JsonSerializer.Deserialize<Dictionary<SkillType, float>>(v)!
    );
}