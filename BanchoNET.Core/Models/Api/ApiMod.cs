using System.Text.Json.Serialization;

namespace BanchoNET.Core.Models.Api;

public class ApiMod : IEquatable<ApiMod>
{
    public string Acronym { get; init; } = "Unknown";
    
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
        var modString = $"{Acronym}";

        modString = Settings.Aggregate(modString,
            (current, setting) => current + $",{setting.Key}={setting.Value}"
        );

        return $"{modString};";
    }
    
    public static bool operator ==(ApiMod? left, ApiMod? right) {
        if (left is null) return right is null;
        return left.Equals(right);
    }
    
    public static bool operator !=(ApiMod? left, ApiMod? right) {
        return !(left == right);
    }
    
    public bool Equals(
        ApiMod? other
    ) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Acronym == other.Acronym;
    }

    public override bool Equals(
        object? obj
    ) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ApiMod)obj);
    }

    public override int GetHashCode() {
        return Acronym.GetHashCode();
    }
}