using System.Net;
using System.Reflection;
using BanchoNET.BeatmapServer.Services;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using Novelog.Abstractions;

namespace BanchoNET.Tests;

public class BeatmapHandlerTests
{
    private Beatmapset? _cached;
    private BeatmapsetDto? _stored;
    private HttpClient _client = null!;
    private BeatmapHandler _handler = null!;
    private int _apiRequests;
    private int _writes;

    [SetUp]
    public void SetUp()
    {
        _stored = new BeatmapsetDto
        {
            Id = 1,
            Beatmaps = [new BeatmapDto { Id = 1, SetId = 1, MD5 = "old" }]
        };
        _cached = new Beatmapset(_stored);
        _apiRequests = 0;
        _writes = 0;
        _client = new HttpClient(new MetadataHandler(() => _apiRequests++));
        var factory = BeatmapTestProxy.Create<IHttpClientFactory>((_, _) => _client);
        var logger = BeatmapTestProxy.Create<ILogger>((_, _) => null);
        var cache = BeatmapTestProxy.Create<IBeatmapService>(CacheCall);
        var repository = BeatmapTestProxy.Create<IBeatmapsRepository>((method, _) =>
        {
            if (method.Name == nameof(IBeatmapsRepository.GetBeatmapset))
                return Task.FromResult(_stored);
            if (method.Name != nameof(IBeatmapsRepository.InsertBeatmapset))
                throw new NotSupportedException(method.Name);

            _writes++;
            return Task.CompletedTask;
        });

        _handler = new BeatmapHandler(logger, cache, repository, _client, factory);
    }

    [TearDown]
    public void TearDown() => _client.Dispose();

    [Test]
    public async Task GetBeatmapset_RefreshesBySetIdAndReturnsUpdatedObject()
    {
        var previous = _cached;
        var result = await _handler.GetBeatmapset(1, recheckApi: true);

        Assert.That(_apiRequests, Is.EqualTo(1));
        Assert.That(result, Is.SameAs(_cached));
        Assert.That(result, Is.Not.SameAs(previous));
        Assert.That(result!.Beatmaps[0].Checksum, Is.EqualTo("new"));
    }

    [Test]
    public async Task GetBeatmap_UnknownChecksumRefreshesUsingSuppliedSetId()
    {
        var result = await _handler.GetBeatmap("new", setId: 1);

        Assert.That(_apiRequests, Is.EqualTo(1));
        Assert.That(result?.Checksum, Is.EqualTo("new"));
    }

    [Test]
    public async Task GetBeatmapset_LoadsDatabaseResultIntoMemory()
    {
        _cached = null;
        var result = await _handler.GetBeatmapset(1);

        Assert.That(result, Is.SameAs(_cached));
        Assert.That(result!.Beatmaps[0].Checksum, Is.EqualTo("old"));
        Assert.That(_apiRequests, Is.Zero);
    }

    [Test]
    public async Task GetBeatmapset_InsertsNewSetOnlyOnce()
    {
        _cached = null;
        _stored = null;
        var result = await _handler.GetBeatmapset(1);

        Assert.That(result, Is.SameAs(_cached));
        Assert.That(_writes, Is.EqualTo(1));
        Assert.That(_apiRequests, Is.EqualTo(1));
    }

    private object? CacheCall(MethodInfo method, object?[]? arguments)
    {
        switch (method.Name)
        {
            case nameof(IBeatmapService.GetBeatmapset):
                return _cached;
            case nameof(IBeatmapService.InsertBeatmapset):
                _cached = (Beatmapset)arguments![0]!;
                return null;
            case nameof(IBeatmapService.GetBeatmap):
                return _cached?.Beatmaps.FirstOrDefault(beatmap => arguments![0] is string checksum
                    ? beatmap.Checksum == checksum
                    : beatmap.Id == (int)arguments![0]!);
            default:
                throw new NotSupportedException(method.Name);
        }
    }

    private sealed class MetadataHandler(Action requested) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"id":1,"ranked":0,"description":{"description":""},
                     "beatmaps":[{"id":1,"beatmapset_id":1,"checksum":"new"}]}
                    """)
            });
        }
    }
}
