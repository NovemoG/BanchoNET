using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Mods.Lazer;

namespace BanchoNET.Core.Models.Api;

public class ApiMod
{
    public string Acronym { get; set; } = "Unknown";
    
    [JsonIgnore]
    public Dictionary<string, object> Settings { get; set; } = new();
    
    [JsonPropertyName("settings"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), JsonInclude]
    private Dictionary<string, object>? SettingsForSerialization
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

    public ApiMod(
        string modString
    ) {
        var modValues = modString.Split(',');
        Acronym = modValues[0];

        foreach (var setting in modValues[1..])
        {
            var kv = setting.Split('=');
            Settings.Add(kv[0], kv[1]);
        }
    }

    public override string ToString() {
        
    }
}