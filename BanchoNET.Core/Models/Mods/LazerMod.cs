using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Mods;

public class LazerMod
{
    public required string Acronym { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Settings? Settings { get; set; } //TODO converter
}