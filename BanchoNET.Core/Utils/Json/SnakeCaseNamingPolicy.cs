using System.Text;
using System.Text.Json;

namespace BanchoNET.Core.Utils.Json;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        DictionaryKeyPolicy = new SnakeCaseNamingPolicy(),
        PropertyNameCaseInsensitive = true
    };
    
    public override string ConvertName(
        string name
    ) {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Contains('_')) return name.ToLowerInvariant();

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]))
                    || i > 0 && i + 1 < name.Length && char.IsLower(name[i + 1]))
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}