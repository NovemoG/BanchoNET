using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api.Scores;

public class MaxStatistics
{
    [JsonPropertyName("great")]
    public int Great { get; set; }
    
    [JsonPropertyName("large_tick_hit")]
    public int LargeTickHit { get; set; }
    
    [JsonPropertyName("ignore_hit")]
    public int IgnoreHit { get; set; }
    
    [JsonPropertyName("slider_tail_hit")]
    public int SliderTailHit { get; set; }
}