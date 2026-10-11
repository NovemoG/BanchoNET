using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using BanchoNET.Core.Utils;

namespace BanchoNET.Tests;

public class BeatmapArchiveTests
{
    private string _directory = null!;
    private string _path = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "1.osz");
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void Version_IgnoresOrderAndChecksumCase()
    {
        var first = new Dictionary<int, string> { [1] = Hash("easy"), [2] = Hash("hard") };
        var second = new Dictionary<int, string> { [2] = Hash("hard").ToLowerInvariant(), [1] = Hash("easy") };

        Assert.That(BeatmapArchive.GetVersion(first), Is.EqualTo(BeatmapArchive.GetVersion(second)));
    }

    [Test]
    public void Version_ChangesWithDifficultyIdsAndMembership()
    {
        var checksums = Checksums("easy", "hard");
        var version = BeatmapArchive.GetVersion(checksums);
        checksums[1] = Hash("updated");
        Assert.That(BeatmapArchive.GetVersion(checksums), Is.Not.EqualTo(version));

        checksums = Checksums("easy", "hard");
        checksums.Remove(2);
        Assert.That(BeatmapArchive.GetVersion(checksums), Is.Not.EqualTo(version));

        checksums = Checksums("easy", "hard");
        checksums[3] = checksums[2];
        checksums.Remove(2);
        Assert.That(BeatmapArchive.GetVersion(checksums), Is.Not.EqualTo(version));
    }

    [Test]
    public async Task IsCurrent_AdoptsExistingArchiveAndPersistsVersion()
    {
        CreateArchive(_path, "easy", "hard");
        var checksums = Checksums("easy", "hard");
        var bytes = await File.ReadAllBytesAsync(_path);

        Assert.That(await BeatmapArchive.IsCurrent(_path, checksums), Is.True);
        Assert.That(File.Exists(_path + ".json"), Is.True);
        Assert.That(await BeatmapArchive.IsCurrent(_path, checksums), Is.True);
        Assert.That(await File.ReadAllBytesAsync(_path), Is.EqualTo(bytes));
    }

    [Test]
    public async Task IsCurrent_RejectsChangedAddedAndRemovedDifficulties()
    {
        CreateArchive(_path, "easy", "hard");
        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("easy", "hard")), Is.True);

        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("easy", "updated")), Is.False);
        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("easy", "hard", "extra")), Is.False);
        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("easy")), Is.False);
    }

    [Test]
    public async Task IsCurrent_RevalidatesWhenManifestIsMissingOrCorrupt()
    {
        CreateArchive(_path, "easy");
        await File.WriteAllTextAsync(_path + ".json", "invalid");
        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("easy")), Is.True);

        File.Delete(_path + ".json");
        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("easy")), Is.True);
    }

    [Test]
    public async Task IsCurrent_DoesNotTrustManifestAfterArchiveReplacement()
    {
        CreateArchive(_path, "easy");
        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("easy")), Is.True);
        CreateArchive(_path, "different beatmap contents");

        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("easy")), Is.False);
    }

    [Test]
    public async Task Matches_RejectsOldMirrorAndDuplicateDifficulties()
    {
        CreateArchive(_path, "easy", "old");
        Assert.That(await BeatmapArchive.Matches(_path, Checksums("easy", "new")), Is.False);

        CreateArchive(_path, "easy", "easy");
        Assert.That(await BeatmapArchive.Matches(_path, Checksums("easy", "hard")), Is.False);
    }

    [Test]
    public async Task Matches_RejectsInvalidArchiveAndEmptyMetadata()
    {
        await File.WriteAllTextAsync(_path, "<html>login required</html>");
        Assert.That(await BeatmapArchive.Matches(_path, Checksums("easy")), Is.False);

        CreateArchive(_path);
        Assert.That(await BeatmapArchive.Matches(_path, new Dictionary<int, string>()), Is.False);
    }

    [Test]
    public async Task Publish_ReplacesArchiveAndStoresOnlyItsVerifiedVersion()
    {
        CreateArchive(_path, "old");
        await BeatmapArchive.IsCurrent(_path, Checksums("old"));
        var tempPath = Path.Combine(_directory, "download.tmp");
        CreateArchive(tempPath, "new");
        var checksums = Checksums("new");

        Assert.That(await BeatmapArchive.Matches(tempPath, checksums), Is.True);
        await BeatmapArchive.Publish(tempPath, _path, BeatmapArchive.GetVersion(checksums));

        Assert.That(await BeatmapArchive.IsCurrent(_path, checksums), Is.True);
        Assert.That(await BeatmapArchive.IsCurrent(_path, Checksums("old")), Is.False);
        Assert.That(File.Exists(tempPath), Is.False);
    }

    private static Dictionary<int, string> Checksums(params string[] contents)
    {
        return contents.Select((content, index) => (Id: index + 1, Hash: Hash(content)))
            .ToDictionary(entry => entry.Id, entry => entry.Hash);
    }

    private static string Hash(string content)
    {
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(content)));
    }

    private static void CreateArchive(string path, params string[] contents)
    {
        using var file = File.Create(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        for (var index = 0; index < contents.Length; index++)
        {
            using var stream = archive.CreateEntry($"{index}.osu").Open();
            stream.Write(Encoding.UTF8.GetBytes(contents[index]));
        }

        using var audio = archive.CreateEntry("audio.mp3").Open();
        audio.Write([1, 2, 3]);
    }
}
