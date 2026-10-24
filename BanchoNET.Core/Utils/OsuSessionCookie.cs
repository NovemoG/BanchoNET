using System.Globalization;

namespace BanchoNET.Core.Utils;

public static class OsuSessionCookie
{
    private static OsuSessionCookieValue _current = new(string.Empty, null);

    public static OsuSessionCookieValue Current => Volatile.Read(ref _current);
    public static event Action? Changed;

    public static void Apply(OsuSessionCookieValue cookie) {
        Volatile.Write(ref _current, cookie);
        Changed?.Invoke();
    }

    public const string Name = "osu_session";
    
    public static readonly TimeSpan WarningPeriod = TimeSpan.FromDays(3);
    
    public static (string Value, DateTimeOffset? ExpiresAt) Parse(
        string? raw
    ) {
        if (string.IsNullOrWhiteSpace(raw))
            return (string.Empty, null);

        raw = raw.Trim();
        
        if (raw.StartsWith("set-cookie:", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("cookie:", StringComparison.OrdinalIgnoreCase))
            raw = raw[(raw.IndexOf(':') + 1)..].Trim();

        var parts = raw.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var session = parts.FirstOrDefault(p => p.StartsWith($"{Name}=", StringComparison.Ordinal));

        if (session == null)
        {
            if (parts.Length > 1 || raw.Contains('='))
                throw new FormatException($"Couldn't find {Name} in the pasted text.");

            return (Validate(raw), null);
        }

        var value = Validate(session[(Name.Length + 1)..]);

        return (value, ReadExpiry(parts));
    }

    /// <summary>
    /// Formats a cookie so that <see cref="Parse"/> reads it back with the same value and expiry.
    /// </summary>
    public static string Format(
        string value,
        DateTimeOffset? expiresAt
    ) {
        if (value.Length == 0) return string.Empty;

        return expiresAt == null
            ? value
            : $"{Name}={value}; expires={expiresAt.Value.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture)}";
    }
    
    public static (string Text, bool Warning)? GetExpiryNote() {
        var cookie = Current;
        if (cookie.Value.Length == 0)
            return null;

        if (cookie.ExpiresAt is not { } expiresAt)
            return ("Expiry unknown. Paste the whole Set-Cookie line of osu_session to have it tracked.", false);

        var remaining = expiresAt - DateTimeOffset.UtcNow;
        var date = expiresAt.ToUniversalTime().ToString("d MMM yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);

        if (remaining <= TimeSpan.Zero)
            return ($"Expired on {date}.", true);

        return ($"Expires on {date}, {FormatRemaining(remaining)} left.", remaining <= WarningPeriod);
    }
    
    public static string? GetReminder() {
        var cookie = Current;
        if (cookie.Value.Length == 0 || cookie.ExpiresAt is not { } expiresAt)
            return null;

        var remaining = expiresAt - DateTimeOffset.UtcNow;
        if (remaining > WarningPeriod)
            return null;

        const string where = "Paste a new one in the admin panel under Session cookie.";

        return remaining <= TimeSpan.Zero
            ? $"The osu! session cookie has expired, so beatmaps are downloaded from the mirror only. {where}"
            : $"The osu! session cookie expires in {FormatRemaining(remaining)}. {where}";
    }

    private static string Validate(
        string value
    ) {
        if (value.Length == 0 || value.Any(c => char.IsWhiteSpace(c) || c is ';' or ',' or '"'))
            throw new FormatException($"The {Name} value contains characters a cookie can't have.");

        return value;
    }

    private static DateTimeOffset? ReadExpiry(
        string[] parts
    ) {
        foreach (var part in parts)
        {
            if (!part.StartsWith("expires=", StringComparison.OrdinalIgnoreCase)) continue;

            if (DateTimeOffset.TryParse(part["expires=".Length..], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expires))
                return expires.ToUniversalTime();
        }

        foreach (var part in parts)
        {
            if (!part.StartsWith("max-age=", StringComparison.OrdinalIgnoreCase)) continue;

            if (long.TryParse(part["max-age=".Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
                return DateTimeOffset.UtcNow.AddSeconds(seconds);
        }

        return null;
    }

    private static string FormatRemaining(
        TimeSpan remaining
    ) {
        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays} days";

        return remaining.TotalHours >= 1
            ? $"{(int)remaining.TotalHours} hours"
            : $"{Math.Max(1, (int)remaining.TotalMinutes)} minutes";
    }
}

public sealed record OsuSessionCookieValue(string Value, DateTimeOffset? ExpiresAt);