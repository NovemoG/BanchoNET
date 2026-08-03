namespace BanchoNET.Core.Utils;

public static class RequestValues
{
    public static bool? ParseBool(
        string? value
    ) {
        if (value == null) return null;

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "on" or "yes" => true,
            "0" or "false" or "off" or "no" or "" => false,
            _ => null
        };
    }
}