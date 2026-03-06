using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Player;

public class StatisticsRulesets
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Statistics? Osu { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Statistics? Taiko { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Statistics? Fruits { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Statistics? Mania { get; set; }
}