using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Mods.Lazer;

namespace BanchoNET.Core.Models.Api;

public class ApiMod
{
    public string Acronym { get; set; } = string.Empty;
    
    [JsonIgnore]
    public Dictionary<string, object> Settings { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? SettingsForSerialization
    {
        get => Settings.Count > 0 ? Settings : null;
        set => Settings = value ?? new Dictionary<string, object>();
    }
    
    [JsonConstructor]
    public ApiMod() { }

    public ApiMod(
        Mod mod
    ) {
        
    }
}