using System.Net;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.BeatmapServer.Services;

public sealed class OsuSessionCookieJar : IDisposable
{
    private static readonly Uri OsuUri = new("https://osu.ppy.sh");
    
    private static readonly TimeSpan MinimumExpiryGain = TimeSpan.FromHours(1);

    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _storeGate = new(1, 1);

    public CookieContainer Container { get; } = new();

    public OsuSessionCookieJar(
        ILogger logger,
        IServiceScopeFactory scopeFactory
    ) {
        _logger = logger;
        _scopeFactory = scopeFactory;

        Apply();
        OsuSessionCookie.Changed += Apply;
    }
    
    public async Task StoreRefreshedCookie() {
        if (GetRefreshedCookie() == null || !await _storeGate.WaitAsync(0))
            return;

        try
        {
            if (GetRefreshedCookie() is not var (value, expiresAt))
                return;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var cookies = scope.ServiceProvider.GetRequiredService<IOsuSessionCookieService>();

            await cookies.Update(OsuSessionCookie.Format(value, expiresAt));

            _logger.LogInfo($"Stored the refreshed osu! session cookie, it now expires {expiresAt:u}", caller: nameof(OsuSessionCookieJar));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Couldn't store the refreshed osu! session cookie", ex, caller: nameof(OsuSessionCookieJar));
        }
        finally
        {
            _storeGate.Release();
        }
    }

    private (string Value, DateTimeOffset ExpiresAt)? GetRefreshedCookie() {
        var cookie = Container.GetCookies(OsuUri)[OsuSessionCookie.Name];
        
        if (cookie == null || cookie.Expired || cookie.Expires == DateTime.MinValue || cookie.Value.Length == 0)
            return null;

        var expiresAt = new DateTimeOffset(cookie.Expires.ToUniversalTime(), TimeSpan.Zero);

        if (OsuSessionCookie.Current.ExpiresAt is { } stored && expiresAt - stored < MinimumExpiryGain)
            return null;

        return (cookie.Value, expiresAt);
    }

    private void Apply() {
        var value = OsuSessionCookie.Current.Value;
        
        var current = Container.GetCookies(OsuUri)[OsuSessionCookie.Name];
        if (current is { Expired: false } && current.Value == value)
            return;

        if (value.Length > 0)
        {
            Container.Add(new Cookie(OsuSessionCookie.Name, value, "/", ".ppy.sh")
            {
                Secure = true,
                HttpOnly = true,
            });
            return;
        }
        
        current?.Expired = true;
    }

    public void Dispose() {
        OsuSessionCookie.Changed -= Apply;
    }
}