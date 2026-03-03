using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BanchoNET.Core.Utils.Json;

public class SimpleDateFormatConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd";

    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) {
        if (reader.TokenType == JsonTokenType.Null)
            throw new JsonException("Null cannot be converted to DateTime.");

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Unexpected token parsing date. Expected String, got {reader.TokenType}.");

        var s = reader.GetString()!;
        
        if (DateTime.TryParseExact(s, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dt))
            return dt;

        throw new JsonException($"Unable to parse \"{s}\" as a date in the format {Format}.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options
    ) {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}