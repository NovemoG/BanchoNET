using System.Net.Http.Json;
using BanchoNET.Core.Utils;

namespace BanchoNET.Infrastructure.Services;

public sealed class OsuTokenProvider(IHttpClientFactory httpClientFactory)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TokenCache? _cache;

    public async Task<string> GetAccessTokenAsync(
        CancellationToken ct
    ) {
        await _gate.WaitAsync(ct);
        try
        {
            if (_cache != null && _cache.ExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1))
                return _cache.AccessToken;

            using var client = httpClientFactory.CreateClient();
            using var content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["client_id"] = AppSettings.OsuClientId,
                    ["client_secret"] = AppSettings.OsuClientSecret,
                    ["grant_type"] = "client_credentials",
                    ["scope"] = "public"
                }
            );

            var response = await client.PostAsync("https://osu.ppy.sh/oauth/token", content, ct);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<OsuTokenResponse>(cancellationToken: ct)
                        ?? throw new Exception("Empty osu token response.");

            _cache = new TokenCache(token.access_token, DateTimeOffset.UtcNow.AddSeconds(token.expires_in));
            return _cache.AccessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record TokenCache(string AccessToken, DateTimeOffset ExpiresAtUtc);
    private sealed record OsuTokenResponse(string access_token, int expires_in);
}