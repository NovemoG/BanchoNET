using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BanchoNET.BeatmapServer.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Novelog.Abstractions;

namespace BanchoNET.Tests;

public class BeatmapDownloaderTests
{
    private string _directory = null!;
    private ServiceProvider _services = null!;
    private HttpClient _client = null!;
    private BeatmapsetDto _set = null!;
    private BeatmapDownloader _downloader = null!;
    private Func<HttpRequestMessage, Task<HttpResponseMessage>> _respond = null!;
    private int _downloads;
    private int _metadataChecks;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
        _set = Set("current");
        _downloads = 0;
        _metadataChecks = 0;
        _respond = _ => Task.FromResult(Response("current"));
        _client = new HttpClient(new DownloadHandler(async request =>
        {
            Interlocked.Increment(ref _downloads);
            return await _respond(request);
        }));

        var repository = BeatmapTestProxy.Create<IBeatmapsRepository>((method, _) =>
            method.Name == nameof(IBeatmapsRepository.GetBeatmapset)
                ? Task.FromResult<BeatmapsetDto?>(_set)
                : throw new NotSupportedException(method.Name));
        var handler = BeatmapTestProxy.Create<IBeatmapHandler>((method, _) =>
        {
            if (method.Name != nameof(IBeatmapHandler.GetBeatmapset))
                throw new NotSupportedException(method.Name);

            Interlocked.Increment(ref _metadataChecks);
            return Task.FromResult<Beatmapset?>(new Beatmapset(_set));
        });

        _services = new ServiceCollection()
            .AddMemoryCache()
            .AddSingleton(repository)
            .AddSingleton(handler)
            .BuildServiceProvider();
        _downloader = CreateDownloader();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _services.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public async Task EnsureBeatmapset_ReusesVerifiedArchiveAcrossRequestsAndRestarts()
    {
        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        Assert.That(_downloads, Is.EqualTo(1));
        Assert.That(_metadataChecks, Is.EqualTo(1));

        _downloader = CreateDownloader();
        _services.GetRequiredService<IMemoryCache>().Remove((nameof(BeatmapDownloader), "metadata", 1));
        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        Assert.That(_downloads, Is.EqualTo(1));
        Assert.That(_metadataChecks, Is.EqualTo(2));
    }

    [Test]
    public async Task EnsureBeatmapset_DownloadsOnceForEachDatabaseVersion()
    {
        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        _set = Set("updated");
        _respond = _ => Task.FromResult(Response("updated"));

        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        Assert.That(_downloads, Is.EqualTo(2));
    }

    [Test]
    public async Task EnsureBeatmapset_SharesDownloadWhenOneCallerCancels()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _respond = async _ =>
        {
            started.SetResult();
            await release.Task;
            return Response("current");
        };
        using var cancellation = new CancellationTokenSource();
        var first = _downloader.EnsureBeatmapset(1, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var others = Enumerable.Range(0, 10).Select(_ => _downloader.EnsureBeatmapset(1)).ToArray();

        cancellation.Cancel();
        release.SetResult();
        Assert.ThrowsAsync<TaskCanceledException>(async () => await first);
        Assert.That(await Task.WhenAll(others), Is.All.True);
        Assert.That(_downloads, Is.EqualTo(1));
    }

    [Test]
    public async Task EnsureBeatmapset_RejectsStaleSourcesAndBacksOffWithoutReplacingCache()
    {
        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        _set = Set("updated");

        Assert.That(await _downloader.EnsureBeatmapset(1), Is.False);
        Assert.That(await _downloader.EnsureBeatmapset(1), Is.False);
        Assert.That(_downloads, Is.EqualTo(3));
        Assert.That(await BeatmapArchive.Matches(PathFor(1), Checksums(Set("current"))), Is.True);
        Assert.That(Directory.GetFiles(_directory, "*.tmp"), Is.Empty);
    }

    [Test]
    public async Task EnsureBeatmapset_FallsBackAfterOfficialDownloadFailure()
    {
        _respond = request => Task.FromResult(request.RequestUri!.Host == "osu.ppy.sh"
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : Response("current"));

        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        Assert.That(_downloads, Is.EqualTo(2));
    }

    [Test]
    public async Task EnsureBeatmapset_DoesNotPublishWhenMetadataChangesDuringDownload()
    {
        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        _set = Set("second");
        _respond = _ =>
        {
            _set = Set("third");
            return Task.FromResult(Response("second"));
        };

        Assert.That(await _downloader.EnsureBeatmapset(1), Is.False);
        Assert.That(await BeatmapArchive.IsCurrent(PathFor(1), Checksums(_set)), Is.False);
        Assert.That(await BeatmapArchive.Matches(PathFor(1), Checksums(Set("current"))), Is.True);
    }

    [Test]
    public async Task EnsureBeatmapset_KeepsPrivateUploadsLocal()
    {
        _set.IsPrivateUpload = true;
        await File.WriteAllBytesAsync(PathFor(1), Archive("private"));

        Assert.That(await _downloader.EnsureBeatmapset(1), Is.True);
        Assert.That(_downloads, Is.Zero);
        Assert.That(_metadataChecks, Is.Zero);
    }

    private BeatmapDownloader CreateDownloader()
    {
        var factory = BeatmapTestProxy.Create<IHttpClientFactory>((_, _) => _client);
        var logger = BeatmapTestProxy.Create<ILogger>((_, _) => null);
        return new BeatmapDownloader(logger, factory, _services.GetRequiredService<IServiceScopeFactory>(),
            _services.GetRequiredService<IMemoryCache>(), PathFor);
    }

    private string PathFor(int id) => Path.Combine(_directory, $"{id}.osz");

    private static BeatmapsetDto Set(string content) => new()
    {
        Id = 1,
        Beatmaps = [new BeatmapDto { Id = 1, SetId = 1, MD5 = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(content))) }]
    };

    private static Dictionary<int, string> Checksums(BeatmapsetDto set) =>
        set.Beatmaps.ToDictionary(beatmap => beatmap.Id, beatmap => beatmap.MD5);

    private static HttpResponseMessage Response(string content) => new(HttpStatusCode.OK)
    {
        Content = new ByteArrayContent(Archive(content))
    };

    private static byte[] Archive(string content)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry("map.osu").Open();
            entry.Write(Encoding.UTF8.GetBytes(content));
        }

        return stream.ToArray();
    }

    private sealed class DownloadHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return respond(request);
        }
    }
}

public class BeatmapTestProxy : DispatchProxy
{
    private Func<MethodInfo, object?[]?, object?> _invoke = null!;

    public static T Create<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = Create<T, BeatmapTestProxy>();
        ((BeatmapTestProxy)(object)proxy)._invoke = invoke;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => _invoke(targetMethod!, args);
}
