using System.Security.Cryptography;
using System.Text.Json;
using BanchoNET.Core.Models;
using BanchoNET.Core.Utils;

namespace BanchoNET.Tests;

[TestFixture]
public class LazerReleaseBundleTests
{
    private string _directory = null!;
    private LazerReleaseBundle _bundle = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "novemo-release-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _bundle = new LazerReleaseBundle
        {
            Version = "2026.1020.1-lazer",
            Channel = "lazer",
            PackageId = "test"
        };

        foreach (var platform in new[] { "win", "linux" })
        {
            var full = Add($"test-{platform}-full.nupkg", platform, "full", "package");
            var feed = new VelopackReleaseFeed
            {
                Assets =
                [
                    new VelopackAsset
                    {
                        PackageId = "test",
                        Version = _bundle.Version,
                        Type = "Full",
                        FileName = full.Name,
                        Size = full.Size,
                        SHA256 = full.SHA256
                    }
                ]
            };

            Add($"releases.{platform}.json", platform, "feed", JsonSerializer.Serialize(feed));
            Add($"portable.{platform}", platform, "portable", "portable");
        }

        Add("setup.exe", "win", "setup", "setup");
    }

    private LazerReleaseFile Add(
        string name,
        string platform,
        string kind,
        string text
    ) {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, text);

        var file = new LazerReleaseFile
        {
            Name = name,
            Platform = platform,
            Kind = kind,
            Size = new FileInfo(path).Length,
            SHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
        };

        _bundle.Files.Add(file);

        return file;
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, true);

    [Test]
    public async Task CompleteBundleIsAccepted() => await _bundle.Validate(_directory, "test");

    [Test]
    public void CorruptPackageIsRejected()
    {
        File.WriteAllText(Path.Combine(_directory, _bundle.Files[0].Name), "changed");

        Assert.ThrowsAsync<InvalidDataException>(() => _bundle.Validate(_directory, "test"));
    }

    [Test]
    public void MissingLinuxFileIsRejected()
    {
        File.Delete(Path.Combine(_directory, _bundle.Single("linux", "portable").Name));

        Assert.ThrowsAsync<FileNotFoundException>(() => _bundle.Validate(_directory, "test"));
    }

    [TestCase("../outside")]
    [TestCase("..\\outside")]
    [TestCase("C:outside")]
    public void TraversalIsRejected(
        string name
    ) {
        _bundle.Files[0].Name = name;

        Assert.ThrowsAsync<InvalidDataException>(() => _bundle.Validate(_directory, "test"));
    }

    [Test]
    public void ChannelMismatchIsRejected()
    {
        _bundle.Channel = "tachyon";

        Assert.ThrowsAsync<InvalidDataException>(() => _bundle.Validate(_directory, "test"));
    }

    [Test]
    public void WrongPackageIdIsRejected() => Assert.ThrowsAsync<InvalidDataException>(
        () => _bundle.Validate(_directory, "other")
    );

    [Test]
    public void FeedReferencingAnotherVersionIsRejected()
    {
        var file = _bundle.Single("win", "feed");
        var path = Path.Combine(_directory, file.Name);
        File.WriteAllText(path, File.ReadAllText(path).Replace(_bundle.Version, "2026.1020.2-lazer"));
        file.Size = new FileInfo(path).Length;
        file.SHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

        Assert.ThrowsAsync<InvalidDataException>(() => _bundle.Validate(_directory, "test"));
    }

    [Test]
    public async Task PublicationIsCompleteAndIdenticalRetryIsIdempotent()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "manifest.json"), JsonSerializer.Serialize(_bundle));

        var published = Path.Combine(_directory, "published");
        await LazerReleasePublisher.Import(_directory, published, "test");

        var path = Path.Combine(published, _bundle.Version);
        var first = await LazerReleaseBundle.Read(path);
        await first.Validate(path, "test");

        await LazerReleasePublisher.Import(_directory, published, "test");

        Assert.That((await LazerReleaseBundle.Read(path)).PublishedAt, Is.EqualTo(first.PublishedAt));
        Assert.That(Directory.GetDirectories(published), Has.Length.EqualTo(1));
    }

    [Test]
    public async Task ExistingVersionCannotBeReplaced()
    {
        var manifest = Path.Combine(_directory, "manifest.json");
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(_bundle));

        var published = Path.Combine(_directory, "published");
        await LazerReleasePublisher.Import(_directory, published, "test");

        _bundle.Commit = "different-commit";
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(_bundle));

        Assert.ThrowsAsync<InvalidDataException>(
            () => LazerReleasePublisher.Import(_directory, published, "test")
        );
        Assert.That(
            (await LazerReleaseBundle.Read(Path.Combine(published, _bundle.Version))).Commit,
            Is.Empty
        );
    }

    [Test]
    public async Task InvalidBundleNeverBecomesVisible()
    {
        await File.WriteAllTextAsync(Path.Combine(_directory, "manifest.json"), JsonSerializer.Serialize(_bundle));
        await File.WriteAllTextAsync(Path.Combine(_directory, _bundle.Files[0].Name), "broken");

        var published = Path.Combine(_directory, "published");
        Assert.ThrowsAsync<InvalidDataException>(
            () => LazerReleasePublisher.Import(_directory, published, "test")
        );
        Assert.That(Directory.Exists(Path.Combine(published, _bundle.Version)), Is.False);
    }
}
