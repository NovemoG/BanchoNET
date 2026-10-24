using System.Security.Cryptography;
using BanchoNET.Core.Utils;
using BanchoNET.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;

namespace BanchoNET.Tests;

[NonParallelizable]
public class OsuSessionCookieServiceTests
{
    private string _directory = null!;
    private string _path = null!;
    private OsuSessionCookieValue _before = null!;

    [SetUp]
    public void SetUp() {
        _directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "cookie.dat");
        _before = OsuSessionCookie.Current;
        OsuSessionCookie.Apply(new OsuSessionCookieValue(string.Empty, null));
    }

    [TearDown]
    public void TearDown() {
        OsuSessionCookie.Apply(_before);
        Directory.Delete(_directory, recursive: true);
    }

    private OsuSessionCookieService CreateService() => new(
        DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(_directory, "keys")),
            builder => builder.SetApplicationName("BanchoNET")), _path);

    [Test]
    public async Task EncryptedCookieAndExpiry_SurviveRestartAndClear() {
        var service = CreateService();
        var expiry = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
        await service.Update(OsuSessionCookie.Format("private-cookie", expiry));
        Assert.That(await File.ReadAllTextAsync(_path), Does.Not.Contain("private-cookie"));

        OsuSessionCookie.Apply(new OsuSessionCookieValue(string.Empty, null));
        await CreateService().Load();
        Assert.That(OsuSessionCookie.Current, Is.EqualTo(new OsuSessionCookieValue("private-cookie", expiry)));

        await service.Clear();
        OsuSessionCookie.Apply(new OsuSessionCookieValue("stale-cookie", expiry));
        await CreateService().Load();
        Assert.That(OsuSessionCookie.Current, Is.EqualTo(new OsuSessionCookieValue(string.Empty, null)));
    }

    [Test]
    public async Task InvalidCookieOrFailedWrite_LeavesSavedAndLiveCookieUnchanged() {
        var service = CreateService();
        await service.Update("original-cookie");
        var saved = await File.ReadAllTextAsync(_path);
        Assert.ThrowsAsync<FormatException>(() => service.Update("invalid cookie"));

        Directory.CreateDirectory(_path + ".tmp");
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.Update("replacement-cookie"));
        Assert.That(OsuSessionCookie.Current.Value, Is.EqualTo("original-cookie"));
        Assert.That(await File.ReadAllTextAsync(_path), Is.EqualTo(saved));
    }

    [Test]
    public async Task CorruptFile_IsRejectedWithoutReplacingLiveCookie() {
        var service = CreateService();
        await service.Update("original-cookie");
        await File.WriteAllTextAsync(_path, "not-protected-data");

        Assert.ThrowsAsync<CryptographicException>(() => CreateService().Load());
        Assert.That(OsuSessionCookie.Current.Value, Is.EqualTo("original-cookie"));
        Assert.That(await File.ReadAllTextAsync(_path), Is.EqualTo("not-protected-data"));
    }
}
