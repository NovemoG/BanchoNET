using System.Text.Json;
using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Players;

namespace BanchoNET.Core.Utils.Json;

public class ApiPlayerConverter : JsonConverter<ApiPlayer>
{
    private const string SnakeName = "rank_history";
    private const string CamelName = "rankHistory";

    public override ApiPlayer? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        
        var localOptions = CloneOptionsWithoutConverter(options, typeof(ApiPlayerConverter));
        var player = JsonSerializer.Deserialize<ApiPlayer>(root.GetRawText(), localOptions);
        
        if (player is { RankHistory: null })
        {
            if (root.TryGetProperty(CamelName, out var altProp))
            {
                var rh = JsonSerializer.Deserialize<RankHistory>(altProp.GetRawText(), localOptions);
                player.RankHistory = rh;
            }
        }

        return player;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ApiPlayer value,
        JsonSerializerOptions options
    ) {
        var localOptions = CloneOptionsWithoutConverter(options, typeof(ApiPlayerConverter));
        var element = JsonSerializer.SerializeToElement(value, localOptions);
        
        writer.WriteStartObject();
        
        foreach (var prop in element.EnumerateObject())
        {
            prop.WriteTo(writer);
        }
        
        var hasSnake = element.TryGetProperty(SnakeName, out var snakeProp);
        var hasCamel = element.TryGetProperty(CamelName, out var camelProp);
        
        if (!hasCamel)
        {
            if (hasSnake)
            {
                writer.WritePropertyName(CamelName);
                snakeProp.WriteTo(writer);
            }
            else if (value.RankHistory != null)
            {
                writer.WritePropertyName(CamelName);
                JsonSerializer.Serialize(writer, value.RankHistory, localOptions);
            }
        }
        
        if (!hasSnake)
        {
            if (hasCamel)
            {
                writer.WritePropertyName(SnakeName);
                camelProp.WriteTo(writer);
            }
            else if (value.RankHistory != null)
            {
                writer.WritePropertyName(SnakeName);
                JsonSerializer.Serialize(writer, value.RankHistory, localOptions);
            }
        }

        writer.WriteEndObject();
    }

    private static JsonSerializerOptions CloneOptionsWithoutConverter(
        JsonSerializerOptions source,
        Type excludeType
    ) {
        var copy = new JsonSerializerOptions
        {
            PropertyNamingPolicy = source.PropertyNamingPolicy,
            DictionaryKeyPolicy = source.DictionaryKeyPolicy,
            PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive,
            WriteIndented = source.WriteIndented,
            DefaultIgnoreCondition = source.DefaultIgnoreCondition
        };

        foreach (var c in source.Converters)
        {
            if (c.GetType() == excludeType) continue;
            copy.Converters.Add(c);
        }

        return copy;
    }
}