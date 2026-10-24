using System.Net;
using BanchoNET.BeatmapServer.Services;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Novelog.Abstractions;

namespace BanchoNET.Tests;

[NonParallelizable]
public class OsuSessionCookieJarTests
{
    [Test]
    public async Task CookieChanges_UpdateDownloadsAndSaveRenewals() {
        var before = OsuSessionCookie.Current;
        string? saved = null;
        var service = BeatmapTestProxy.Create<IOsuSessionCookieService>((method, args) =>
        {
            Assert.That(method.Name, Is.EqualTo(nameof(IOsuSessionCookieService.Update)));
            saved = (string)args![0]!;
            var (value, expiresAt) = OsuSessionCookie.Parse(saved);
            OsuSessionCookie.Apply(new OsuSessionCookieValue(value, expiresAt));
            return Task.CompletedTask;
        });
        using var services = new ServiceCollection().AddSingleton(service).BuildServiceProvider();
        var logger = BeatmapTestProxy.Create<ILogger>((_, _) => null);
        var uri = new Uri("https://osu.ppy.sh");

        try
        {
            OsuSessionCookie.Apply(new OsuSessionCookieValue("original", DateTimeOffset.UtcNow.AddDays(1)));
            using var jar = new OsuSessionCookieJar(logger, services.GetRequiredService<IServiceScopeFactory>());

            OsuSessionCookie.Apply(new OsuSessionCookieValue("replacement", DateTimeOffset.UtcNow.AddDays(2)));
            Assert.That(jar.Container.GetCookies(uri)[OsuSessionCookie.Name]!.Value, Is.EqualTo("replacement"));

            jar.Container.Add(new Cookie(OsuSessionCookie.Name, "renewed", "/", ".ppy.sh")
            {
                Expires = DateTime.UtcNow.AddDays(30),
            });
            await jar.StoreRefreshedCookie();

            Assert.That(saved, Is.Not.Null);
            Assert.That(OsuSessionCookie.Parse(saved).Value, Is.EqualTo("renewed"));
            Assert.That(OsuSessionCookie.Current.ExpiresAt, Is.GreaterThan(DateTimeOffset.UtcNow.AddDays(29)));
            Assert.That(OsuSessionCookie.GetReminder(), Is.Null);

            OsuSessionCookie.Apply(new OsuSessionCookieValue("renewed", DateTimeOffset.UtcNow.AddDays(1)));
            Assert.That(OsuSessionCookie.GetReminder(), Does.Contain("expires in"));

            OsuSessionCookie.Apply(new OsuSessionCookieValue(string.Empty, null));
            Assert.That(jar.Container.GetCookies(uri)[OsuSessionCookie.Name], Is.Null);
            Assert.That(OsuSessionCookie.GetReminder(), Is.Null);
        }
        finally
        {
            OsuSessionCookie.Apply(before);
        }
    }
}
