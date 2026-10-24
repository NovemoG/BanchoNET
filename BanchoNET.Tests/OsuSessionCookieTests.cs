using BanchoNET.Core.Utils;

namespace BanchoNET.Tests;

public class OsuSessionCookieTests
{
    [Test]
    public void SetCookieHeader_KeepsValueAndExpiry()
    {
        var (value, expiresAt) = OsuSessionCookie.Parse(
            "osu_session=cookie%3D; expires=Mon, 23 Nov 2026 04:29:12 GMT; Max-Age=2592000; path=/; domain=.ppy.sh; secure; httponly; samesite=lax");

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo("cookie%3D"));
            Assert.That(expiresAt, Is.EqualTo(new DateTimeOffset(2026, 11, 23, 4, 29, 12, TimeSpan.Zero)));
        });
    }

    [Test]
    public void HeaderName_IsIgnored()
    {
        var (value, expiresAt) = OsuSessionCookie.Parse("set-cookie: osu_session=abc; Max-Age=60");

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo("abc"));
            Assert.That(expiresAt, Is.EqualTo(DateTimeOffset.UtcNow.AddSeconds(60)).Within(TimeSpan.FromSeconds(5)));
        });
    }

    [Test]
    public void RequestCookieHeader_HasNoExpiry()
    {
        var (value, expiresAt) = OsuSessionCookie.Parse("XSRF-TOKEN=x; osu_session=abc; locale=en");

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo("abc"));
            Assert.That(expiresAt, Is.Null);
        });
    }

    [Test]
    public void BareValue_IsKept()
    {
        Assert.That(OsuSessionCookie.Parse("  abc%3D  ").Value, Is.EqualTo("abc%3D"));
    }

    [Test]
    public void OtherCookies_AreRejected()
    {
        Assert.Throws<FormatException>(() => OsuSessionCookie.Parse("XSRF-TOKEN=x; locale=en"));
    }

    [Test]
    public void Format_RoundTrips()
    {
        var expiresAt = new DateTimeOffset(2026, 11, 23, 4, 29, 12, TimeSpan.Zero);
        var (value, parsedExpiry) = OsuSessionCookie.Parse(OsuSessionCookie.Format("abc", expiresAt));

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo("abc"));
            Assert.That(parsedExpiry, Is.EqualTo(expiresAt));
        });
    }
}
