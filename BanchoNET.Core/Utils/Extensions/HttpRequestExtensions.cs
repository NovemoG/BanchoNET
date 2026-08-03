using System.Net;
using Microsoft.AspNetCore.Http;
using BanchoNET.Core.Models.Auth;

namespace BanchoNET.Core.Utils.Extensions;

public static class HttpRequestExtensions
{
    public static string? GetClientIp(
        this HttpRequest request
    ) {
        var headers = request.Headers;

        var candidates = new[]
        {
            headers.TryGetValue("CF-Connecting-IP", out var cfIp) ? cfIp.ToString() : null,
            headers.TryGetValue("X-Forwarded-For", out var xff) ? xff.ToString().Split(',')[0].Trim() : null,
            headers.TryGetValue("X-Real-IP", out var xRealIp) ? xRealIp.ToString() : null
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            if (IPAddress.TryParse(candidate, out var parsed)) return parsed.ToString();
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    public static SessionOrigin GetSessionOrigin(
        this HttpRequest request
    ) {
        var userAgent = request.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null;

        return new SessionOrigin(
            request.GetClientIp(),
            string.IsNullOrWhiteSpace(userAgent) ? null : Truncate(userAgent, 512));
    }

    private static string Truncate(
        string value,
        int max
    ) => value.Length <= max ? value : value[..max];
}